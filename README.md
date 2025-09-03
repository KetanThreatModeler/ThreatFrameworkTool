# ThreatFramework Index Service

This solution provides a high-performance in-memory caching service for threat modeling index data loaded from YAML files.

## Features

- **High-Performance Lookups**: Optimized data structures for fast O(1) lookups
- **Multiple Lookup Methods**: Support for GUID-based and kind+GUID-based lookups
- **Hot Reload**: Refresh data without restarting the application
- **Thread-Safe**: Concurrent access support using lock-free data structures
- **RESTful API**: Clean REST endpoints for all operations
- **Comprehensive Logging**: Detailed logging for monitoring and debugging
- **SOLID Principles**: Clean architecture following dependency injection patterns

## Architecture

### Components

1. **ThreatFramework.Infrastructure**
   - `IndexService`: Core service for YAML processing and caching
   - `IIndexService`: Interface for dependency injection
   - `ThreatModelingOptions`: Configuration settings
   - `IndexItem`: Domain model for YAML data items

2. **ThreatFramework.API**
   - `IndexController`: REST API endpoints
   - Service registration and dependency injection

### Data Structures

The service uses optimized concurrent dictionaries for different lookup patterns:
- `ConcurrentDictionary<Guid, IndexItem>`: GUID to full item mapping
- `ConcurrentDictionary<Guid, int>`: GUID to ID mapping
- `ConcurrentDictionary<string, ConcurrentDictionary<Guid, int>>`: Kind+GUID to ID mapping
- `ConcurrentDictionary<string, List<IndexItem>>`: Kind to items mapping

## Configuration

### appsettings.json
```json
{
  "ThreatModeling": {
    "IndexYamlPath": "..\\ThreatFramework.Infrastructure\\Resources\\index.yaml"
  }
}
```

The `IndexYamlPath` can be:
- Relative path (resolved from application base directory)
- Absolute path

## API Endpoints

### POST /api/index/refresh
Refreshes the in-memory cache from the YAML file.

**Response:**
```json
{
  "success": true,
  "message": "Index cache refreshed successfully",
  "statistics": {
    "totalItems": 322,
    "itemsByKind": {
      "component": 322,
      "library": 1,
      "property": 262
    },
    "lastRefreshed": "2024-01-15T10:30:00Z",
    "yamlFilePath": "D:\\Path\\To\\index.yaml"
  }
}
```

### GET /api/index/statistics
Gets current cache statistics.

### GET /api/index/lookup/by-guid/{guid}
Looks up an ID by GUID.

**Example:** `GET /api/index/lookup/by-guid/3b913906-b137-4f4b-9804-6d444cf9a8d0`

**Response:**
```json
{
  "guid": "3b913906-b137-4f4b-9804-6d444cf9a8d0",
  "id": 1
}
```

### GET /api/index/lookup/by-kind-guid/{kind}/{guid}
Looks up an ID by kind and GUID combination.

**Example:** `GET /api/index/lookup/by-kind-guid/component/3b913906-b137-4f4b-9804-6d444cf9a8d0`

### GET /api/index/item/{guid}
Gets full item details by GUID.

**Response:**
```json
{
  "kind": "component",
  "guid": "3b913906-b137-4f4b-9804-6d444cf9a8d0",
  "id": 1,
  "name": "2G test"
}
```

### GET /api/index/items/by-kind/{kind}
Gets all items of a specific kind.

**Example:** `GET /api/index/items/by-kind/component`

## Performance Characteristics

- **Lookup Time**: O(1) for all operations
- **Memory Usage**: Optimized with shared references to avoid duplication
- **Concurrency**: Lock-free reads, single-writer refresh operations
- **Startup Time**: Lazy initialization with background loading

## Usage Examples

### C# Service Injection
```csharp
public class MyService
{
    private readonly IIndexService _indexService;
    
    public MyService(IIndexService indexService)
    {
        _indexService = indexService;
    }
    
    public async Task<int?> GetComponentId(Guid componentGuid)
    {
        return _indexService.GetIdByKindAndGuid("component", componentGuid);
    }
}
```

### HTTP Client Usage
```csharp
// Refresh cache
await httpClient.PostAsync("/api/index/refresh", null);

// Lookup by GUID
var response = await httpClient.GetAsync($"/api/index/lookup/by-guid/{guid}");
var result = await response.Content.ReadFromJsonAsync<LookupResult>();
```

## Error Handling

- **File Not Found**: Returns detailed error messages
- **Invalid YAML**: Logged with specific parsing errors  
- **Service Unavailable**: Graceful degradation with appropriate HTTP status codes
- **Concurrent Access**: Thread-safe operations with proper exception handling

## Monitoring

The service provides comprehensive logging at different levels:
- **Information**: Successful operations and statistics
- **Warning**: Non-critical issues like missing GUIDs
- **Error**: Failed operations with full exception details

## Best Practices

1. **Production Deployment**: Use absolute paths for YAML files
2. **Monitoring**: Set up alerts on refresh failures
3. **Performance**: Monitor cache hit rates and refresh frequency
4. **Security**: Restrict access to refresh endpoints in production
5. **Backup**: Ensure YAML files are version controlled and backed up