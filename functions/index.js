const { onSchedule } = require("firebase-functions/v2/scheduler");
const { defineSecret } = require("firebase-functions/params");
const admin = require("firebase-admin");
const nodemailer = require("nodemailer");

admin.initializeApp();

const db = admin.firestore();
const GMAIL_APP_PASSWORD = defineSecret("GMAIL_APP_PASSWORD");

// Runs every day at midnight UTC
exports.deleteInactiveUsers = onSchedule(
    { schedule: "every 24 hours", secrets: [GMAIL_APP_PASSWORD] },
    async (event) => {
        const now = Date.now();
        const ninetyDaysMs      = 90 * 24 * 60 * 60 * 1000;
        const eightyThreeDaysMs = 83 * 24 * 60 * 60 * 1000;

        const deleteCutoff  = new Date(now - ninetyDaysMs);
        const warnCutoff    = new Date(now - eightyThreeDaysMs);
        const warnCutoffEnd = new Date(now - eightyThreeDaysMs + 24 * 60 * 60 * 1000);

        console.log(`[deleteInactiveUsers] Running. Delete cutoff: ${deleteCutoff.toISOString()}`);

        const transporter = nodemailer.createTransport({
            service: "gmail",
            auth: {
                user: "morrowmoss.studios@gmail.com",
                pass: GMAIL_APP_PASSWORD.value(),
            },
        });

        const playersRef = db.collection("players");

        // Warning emails (day 83)
        const warnSnapshot = await playersRef
            .where("lastUpdated", "<", warnCutoffEnd)
            .where("lastUpdated", ">=", warnCutoff)
            .get();

        let warnCount = 0;
        for (const doc of warnSnapshot.docs) {
            const data = doc.data();
            const email = data.email;
            const name  = data.displayName || "Adventurer";

            if (!email) continue;

            try {
                await transporter.sendMail({
                    from: '"MorrowMoss Studios" <morrowmoss.studios@gmail.com>',
                    to: email,
                    subject: "Your TriviaForge account will be deleted in 7 days",
                    text: `Hi ${name},\n\nWe noticed you haven't played TriviaForge in a while. Your account and all associated data will be permanently deleted in 7 days due to inactivity.\n\nIf you'd like to keep your account, simply open TriviaForge and play a round before then.\n\nIf you have any questions, contact us at support@morrowmoss.com.\n\nThanks for playing,\nMorrowMoss Studios`,
                });
                warnCount++;
            } catch (err) {
                console.error(`[deleteInactiveUsers] Failed to send warning to ${email}: ${err.message}`);
            }
        }

        console.log(`[deleteInactiveUsers] Sent ${warnCount} warning email(s).`);

        // Delete inactive accounts (day 90+)
        const deleteSnapshot = await playersRef
            .where("lastUpdated", "<", deleteCutoff)
            .get();

        if (deleteSnapshot.empty) {
            console.log("[deleteInactiveUsers] No accounts to delete.");
            return null;
        }

        const deletedUids = new Set();
        const batch = db.batch();
        let deleteCount = 0;

        deleteSnapshot.forEach((doc) => {
            batch.delete(doc.ref);
            deletedUids.add(doc.id);
            deleteCount++;
        });

        await batch.commit();
        console.log(`[deleteInactiveUsers] Deleted ${deleteCount} inactive account(s).`);

        // Clean up username mappings
        const usernamesRef  = db.collection("usernames");
        const usernameDocs  = await usernamesRef.get();
        const usernameBatch = db.batch();
        let usernameCount   = 0;

        usernameDocs.forEach((doc) => {
            const data = doc.data();
            if (data.uid && deletedUids.has(data.uid)) {
                usernameBatch.delete(doc.ref);
                usernameCount++;
            }
        });

        if (usernameCount > 0) {
            await usernameBatch.commit();
            console.log(`[deleteInactiveUsers] Deleted ${usernameCount} username mapping(s).`);
        }

        return null;
    }
);