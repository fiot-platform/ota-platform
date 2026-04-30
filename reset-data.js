// ─── OTAPlatform — Data Reset (Users preserved) ──────────────────────────────
// Deletes all documents from every collection EXCEPT Users.
// Indexes and collection structure are preserved.
//
//   mongosh "mongodb://iot.ssmsportal.com:39999/?replicaSet=fiot-rs" reset-data.js
// ─────────────────────────────────────────────────────────────────────────────

use("OTAPlatform");

const collections = [
  "Devices",
  "OtaJobs",
  "Firmwares",
  "Rollouts",
  "RolloutPolicies",
  "RepositoryMasters",
  "RepositoryEvents",
  "Projects",
  "clients",
  "AuditLogs",
  "DeviceOtaEvents",
];

print("\n=== OTAPlatform Data Reset (Users preserved) ===\n");

collections.forEach((name) => {
  try {
    const result = db.getCollection(name).deleteMany({});
    print(`  ✓  ${name.padEnd(22)} ${result.deletedCount} document(s) deleted`);
  } catch (e) {
    print(`  ✗  ${name.padEnd(22)} ERROR: ${e.message}`);
  }
});

print("\nDone. Users collection untouched.\n");
