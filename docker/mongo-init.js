// MongoDB initialization script for production
db = db.getSiblingDB('EntitiesManagerDb');

// Create application user
db.createUser({
  user: 'entitiesmanager',
  pwd: 'entitiesmanager123',
  roles: [
    {
      role: 'readWrite',
      db: 'EntitiesManagerDb'
    }
  ]
});

// Create collections with validation
db.createCollection('sources', {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: ['_id', 'address', 'version', 'name', 'createdAt', 'createdBy'],
      properties: {
        _id: {
          bsonType: 'string',
          description: 'must be a string and is required'
        },
        address: {
          bsonType: 'string',
          description: 'must be a string and is required'
        },
        version: {
          bsonType: 'string',
          description: 'must be a string and is required'
        },
        name: {
          bsonType: 'string',
          description: 'must be a string and is required'
        },
        configuration: {
          bsonType: 'object',
          description: 'must be an object'
        },
        createdAt: {
          bsonType: 'date',
          description: 'must be a date and is required'
        },
        createdBy: {
          bsonType: 'string',
          description: 'must be a string and is required'
        },
        updatedAt: {
          bsonType: 'date',
          description: 'must be a date'
        },
        updatedBy: {
          bsonType: 'string',
          description: 'must be a string'
        }
      }
    }
  }
});

db.createCollection('destinations', {
  validator: {
    $jsonSchema: {
      bsonType: 'object',
      required: ['_id', 'name', 'version', 'inputSchema', 'createdAt', 'createdBy'],
      properties: {
        _id: {
          bsonType: 'string',
          description: 'must be a string and is required'
        },
        name: {
          bsonType: 'string',
          description: 'must be a string and is required'
        },
        version: {
          bsonType: 'string',
          description: 'must be a string and is required'
        },
        inputSchema: {
          bsonType: 'string',
          description: 'must be a string and is required'
        },
        createdAt: {
          bsonType: 'date',
          description: 'must be a date and is required'
        },
        createdBy: {
          bsonType: 'string',
          description: 'must be a string and is required'
        },
        updatedAt: {
          bsonType: 'date',
          description: 'must be a date'
        },
        updatedBy: {
          bsonType: 'string',
          description: 'must be a string'
        }
      }
    }
  }
});

// Create indexes
db.sources.createIndex({ 'address': 1, 'version': 1 }, { unique: true });
db.sources.createIndex({ 'createdAt': 1 });
db.sources.createIndex({ 'updatedAt': 1 });

db.destinations.createIndex({ 'name': 1, 'version': 1 }, { unique: true });
db.destinations.createIndex({ 'createdAt': 1 });
db.destinations.createIndex({ 'updatedAt': 1 });

print('Database initialization completed successfully');
