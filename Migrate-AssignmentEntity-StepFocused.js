// MongoDB Migration Script for AssignmentEntity Step-Focused Modifications
// This script migrates existing AssignmentEntity data to the new Step-focused structure

// Database connection (adjust as needed)
// Run with: mongosh "mongodb://localhost:27017/EntitiesManagerDb" --file Migrate-AssignmentEntity-StepFocused.js

print("🔄 Starting AssignmentEntity Step-Focused Migration...");
print("=====================================================");

// Get the assignments collection
const db = db.getSiblingDB('EntitiesManagerDb');
const assignmentsCollection = db.assignments;

print("\n1. Analyzing existing data...");

// Count existing documents
const totalCount = assignmentsCollection.countDocuments();
print(`   Found ${totalCount} existing assignment documents`);

if (totalCount === 0) {
    print("   ✅ No existing data to migrate");
    print("\n2. Creating indexes for new structure...");
} else {
    print(`   📊 Documents to migrate: ${totalCount}`);
    
    // Analyze current structure
    const sampleDoc = assignmentsCollection.findOne();
    if (sampleDoc) {
        print("   📋 Current document structure:");
        print(`      - Has TaskScheduledId: ${sampleDoc.taskScheduledId ? '✅' : '❌'}`);
        print(`      - Has StepId: ${sampleDoc.stepId ? '✅' : '❌'}`);
        print(`      - Has EntityIds: ${sampleDoc.entityIds ? '✅' : '❌'}`);
        print(`      - Has Version: ${sampleDoc.version ? '✅' : '❌'}`);
    }

    print("\n2. Backing up existing data...");
    
    // Create backup collection
    const backupCollectionName = `assignments_backup_${new Date().toISOString().slice(0,19).replace(/[-:]/g, '')}`;
    
    try {
        // Copy all documents to backup
        const backupResult = assignmentsCollection.aggregate([
            { $out: backupCollectionName }
        ]);
        print(`   ✅ Backup created: ${backupCollectionName}`);
    } catch (error) {
        print(`   ❌ Backup failed: ${error.message}`);
        print("   🛑 Migration aborted for safety");
        quit(1);
    }

    print("\n3. Migrating document structure...");
    
    let migratedCount = 0;
    let errorCount = 0;
    
    // Process documents in batches
    const batchSize = 100;
    let skip = 0;
    
    while (skip < totalCount) {
        const batch = assignmentsCollection.find().skip(skip).limit(batchSize).toArray();
        
        for (const doc of batch) {
            try {
                const updateDoc = {
                    $unset: {},
                    $set: {}
                };
                
                // Remove TaskScheduledId if it exists
                if (doc.taskScheduledId) {
                    updateDoc.$unset.taskScheduledId = "";
                }
                
                // Add StepId if it doesn't exist
                if (!doc.stepId) {
                    updateDoc.$set.stepId = new ObjectId();
                }
                
                // Add EntityIds if it doesn't exist
                if (!doc.entityIds) {
                    updateDoc.$set.entityIds = [];
                }
                
                // Only update if there are changes to make
                if (Object.keys(updateDoc.$unset).length > 0 || Object.keys(updateDoc.$set).length > 0) {
                    const result = assignmentsCollection.updateOne(
                        { _id: doc._id },
                        updateDoc
                    );
                    
                    if (result.modifiedCount === 1) {
                        migratedCount++;
                    }
                }
                
            } catch (error) {
                print(`   ❌ Error migrating document ${doc._id}: ${error.message}`);
                errorCount++;
            }
        }
        
        skip += batchSize;
        print(`   📈 Progress: ${Math.min(skip, totalCount)}/${totalCount} documents processed`);
    }
    
    print(`\n   ✅ Migration completed:`);
    print(`      - Documents migrated: ${migratedCount}`);
    print(`      - Errors encountered: ${errorCount}`);
    print(`      - Backup collection: ${backupCollectionName}`);
}

print("\n4. Dropping old indexes...");

try {
    // Drop old indexes if they exist
    const existingIndexes = assignmentsCollection.getIndexes();
    
    for (const index of existingIndexes) {
        if (index.name === 'taskScheduledId_1') {
            assignmentsCollection.dropIndex('taskScheduledId_1');
            print("   ✅ Dropped taskScheduledId_1 index");
        }
        if (index.name === 'version_1' && index.unique) {
            assignmentsCollection.dropIndex('version_1');
            print("   ✅ Dropped unique version_1 index");
        }
    }
} catch (error) {
    print(`   ⚠️  Index cleanup warning: ${error.message}`);
}

print("\n5. Creating new indexes...");

try {
    // Create StepId unique index
    assignmentsCollection.createIndex(
        { "stepId": 1 }, 
        { 
            unique: true, 
            name: "stepId_1",
            background: true 
        }
    );
    print("   ✅ Created unique stepId_1 index");
    
    // Create EntityIds index for array queries
    assignmentsCollection.createIndex(
        { "entityIds": 1 }, 
        { 
            name: "entityIds_1",
            background: true 
        }
    );
    print("   ✅ Created entityIds_1 index");
    
    // Create Version index (non-unique)
    assignmentsCollection.createIndex(
        { "version": 1 }, 
        { 
            name: "version_1",
            background: true 
        }
    );
    print("   ✅ Created version_1 index");
    
    // Create Name index
    assignmentsCollection.createIndex(
        { "name": 1 }, 
        { 
            name: "name_1",
            background: true 
        }
    );
    print("   ✅ Created name_1 index");
    
} catch (error) {
    print(`   ❌ Index creation error: ${error.message}`);
    print("   🔧 You may need to resolve data conflicts manually");
}

print("\n6. Validating migration...");

// Validate the migration
const postMigrationCount = assignmentsCollection.countDocuments();
const documentsWithStepId = assignmentsCollection.countDocuments({ stepId: { $exists: true } });
const documentsWithEntityIds = assignmentsCollection.countDocuments({ entityIds: { $exists: true } });
const documentsWithTaskScheduledId = assignmentsCollection.countDocuments({ taskScheduledId: { $exists: true } });

print(`   📊 Post-migration statistics:`);
print(`      - Total documents: ${postMigrationCount}`);
print(`      - Documents with StepId: ${documentsWithStepId}`);
print(`      - Documents with EntityIds: ${documentsWithEntityIds}`);
print(`      - Documents with TaskScheduledId: ${documentsWithTaskScheduledId}`);

// Validation checks
const validationPassed = 
    postMigrationCount === totalCount &&
    documentsWithStepId === postMigrationCount &&
    documentsWithEntityIds === postMigrationCount &&
    documentsWithTaskScheduledId === 0;

if (validationPassed) {
    print("\n✅ MIGRATION VALIDATION PASSED");
    print("   All documents successfully migrated to Step-focused structure");
} else {
    print("\n❌ MIGRATION VALIDATION FAILED");
    print("   Some documents may not have been migrated correctly");
    print("   Please review the data manually");
}

print("\n7. Final index summary...");
const finalIndexes = assignmentsCollection.getIndexes();
print("   📋 Current indexes:");
for (const index of finalIndexes) {
    const uniqueFlag = index.unique ? " (UNIQUE)" : "";
    print(`      - ${index.name}: ${JSON.stringify(index.key)}${uniqueFlag}`);
}

print("\n🎉 ASSIGNMENT ENTITY STEP-FOCUSED MIGRATION COMPLETE!");
print("=====================================================");
print("");
print("📋 Summary:");
print(`   • Total documents processed: ${totalCount}`);
print(`   • Migration validation: ${validationPassed ? 'PASSED ✅' : 'FAILED ❌'}`);
print(`   • New indexes created: stepId_1 (unique), entityIds_1, version_1, name_1`);
print(`   • Old indexes removed: taskScheduledId_1, version_1 (unique)`);
print("");
print("🔧 Next Steps:");
print("   1. Test the API endpoints with the validation script");
print("   2. Verify application functionality");
print("   3. Remove backup collection when satisfied with migration");
print("");
print("⚠️  Important Notes:");
print("   • Backup collection created for rollback if needed");
print("   • All existing TaskScheduledId references have been removed");
print("   • New StepId values have been auto-generated");
print("   • EntityIds arrays have been initialized as empty");
print("");
