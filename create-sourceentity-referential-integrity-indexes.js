// MongoDB Index Creation Script for SourceEntity Referential Integrity
// This script creates the necessary indexes for efficient SourceEntity referential integrity validation

// Connect to the EntitiesManagerDb database
use EntitiesManagerDb;

print("=== SOURCEENTITY REFERENTIAL INTEGRITY INDEX CREATION ===");
print("Creating indexes for efficient SourceEntity referential integrity validation...");
print("");

// ===================================================================
// 1. SCHEDULEDFLOWENTITY.SOURCEID INDEX
// ===================================================================

print("1. Creating ScheduledFlowEntity.SourceId index...");

try {
    // Create index for ScheduledFlowEntity.SourceId for efficient lookups
    var result = db.scheduledflows.createIndex(
        { "sourceId": 1 }, 
        { 
            name: "idx_scheduledflows_sourceId",
            background: true,
            comment: "Index for SourceEntity referential integrity validation - enables efficient lookup of ScheduledFlowEntity records by SourceId"
        }
    );
    
    print("✅ Successfully created ScheduledFlowEntity.SourceId index: " + result);
} catch (error) {
    if (error.code === 85) { // IndexOptionsConflict - index already exists
        print("⚠️  ScheduledFlowEntity.SourceId index already exists, skipping...");
    } else {
        print("❌ Error creating ScheduledFlowEntity.SourceId index: " + error.message);
        throw error;
    }
}

print("");

// ===================================================================
// 2. VERIFY INDEX CREATION
// ===================================================================

print("2. Verifying index creation...");

try {
    var indexes = db.scheduledflows.getIndexes();
    var sourceIdIndex = indexes.find(function(index) {
        return index.name === "idx_scheduledflows_sourceId";
    });
    
    if (sourceIdIndex) {
        print("✅ ScheduledFlowEntity.SourceId index verified:");
        print("   - Name: " + sourceIdIndex.name);
        print("   - Key: " + JSON.stringify(sourceIdIndex.key));
        print("   - Background: " + (sourceIdIndex.background || false));
    } else {
        print("❌ ScheduledFlowEntity.SourceId index not found!");
    }
} catch (error) {
    print("❌ Error verifying indexes: " + error.message);
}

print("");

// ===================================================================
// 3. PERFORMANCE TEST
// ===================================================================

print("3. Testing index performance...");

try {
    // Test query performance with explain
    var sampleSourceId = ObjectId();
    var explainResult = db.scheduledflows.find({ "sourceId": sampleSourceId }).explain("executionStats");
    
    print("✅ Query execution plan:");
    print("   - Winning plan: " + explainResult.queryPlanner.winningPlan.stage);
    print("   - Index used: " + (explainResult.queryPlanner.winningPlan.indexName || "No index"));
    print("   - Execution time: " + (explainResult.executionStats.executionTimeMillis || 0) + "ms");
    
    if (explainResult.queryPlanner.winningPlan.stage === "IXSCAN") {
        print("✅ Index is being used correctly for SourceId queries");
    } else {
        print("⚠️  Index may not be optimal - review query plan");
    }
} catch (error) {
    print("⚠️  Error testing index performance: " + error.message);
}

print("");

// ===================================================================
// 4. COLLECTION STATISTICS
// ===================================================================

print("4. Collection statistics...");

try {
    var stats = db.scheduledflows.stats();
    print("✅ ScheduledFlowEntity collection statistics:");
    print("   - Document count: " + stats.count);
    print("   - Average document size: " + Math.round(stats.avgObjSize) + " bytes");
    print("   - Total index size: " + Math.round(stats.totalIndexSize / 1024) + " KB");
    print("   - Index count: " + stats.nindexes);
} catch (error) {
    print("⚠️  Error retrieving collection statistics: " + error.message);
}

print("");

// ===================================================================
// 5. CONFIGURATION RECOMMENDATIONS
// ===================================================================

print("5. Configuration recommendations...");
print("✅ Add the following to appsettings.json:");
print('   "ReferentialIntegrity": {');
print('     "ValidateScheduledFlowReferences": true,');
print('     "SourceEntityValidationTimeout": 30000,');
print('     "EnableSourceEntityValidationLogging": true');
print('   }');

print("");
print("=== INDEX CREATION COMPLETE ===");
print("SourceEntity referential integrity indexes have been created successfully!");
print("The system is now ready for SourceEntity referential integrity validation.");
print("");
print("Next steps:");
print("1. Deploy the updated application code");
print("2. Update configuration settings");
print("3. Run integration tests to verify functionality");
print("4. Monitor performance metrics");
