// MongoDB script to create indexes for referential integrity validation
// Run this script against the EntitiesManagerDb database

print("Creating indexes for referential integrity validation...");

// Switch to the EntitiesManagerDb database
use EntitiesManagerDb;

// Create index on sources.protocolId for fast lookups
print("Creating index on sources.protocolId...");
db.sources.createIndex({ "protocolId": 1 }, { 
    name: "idx_sources_protocolId",
    background: true 
});

// Create index on destinations.protocolId for fast lookups
print("Creating index on destinations.protocolId...");
db.destinations.createIndex({ "protocolId": 1 }, { 
    name: "idx_destinations_protocolId",
    background: true 
});

// Verify indexes were created
print("\nVerifying indexes...");

print("Sources collection indexes:");
db.sources.getIndexes().forEach(function(index) {
    if (index.name.includes("protocolId")) {
        print("  ✓ " + index.name + ": " + JSON.stringify(index.key));
    }
});

print("Destinations collection indexes:");
db.destinations.getIndexes().forEach(function(index) {
    if (index.name.includes("protocolId")) {
        print("  ✓ " + index.name + ": " + JSON.stringify(index.key));
    }
});

// Test index performance
print("\nTesting index performance...");

// Create a sample protocol for testing (if it doesn't exist)
var testProtocolId = ObjectId();
print("Using test protocol ID: " + testProtocolId);

// Test source query performance
print("Testing sources query performance...");
var sourceExplain = db.sources.find({ "protocolId": testProtocolId }).explain("executionStats");
print("  Sources query execution time: " + sourceExplain.executionStats.executionTimeMillis + "ms");
print("  Index used: " + (sourceExplain.executionStats.indexesUsed ? "Yes" : "No"));

// Test destination query performance
print("Testing destinations query performance...");
var destinationExplain = db.destinations.find({ "protocolId": testProtocolId }).explain("executionStats");
print("  Destinations query execution time: " + destinationExplain.executionStats.executionTimeMillis + "ms");
print("  Index used: " + (destinationExplain.executionStats.indexesUsed ? "Yes" : "No"));

print("\n✅ Referential integrity indexes created successfully!");
print("📊 Performance optimization complete for ProtocolEntity validation");
