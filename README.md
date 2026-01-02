# Availability Service

## Overview

The Availability Service manages provider availability, working hours, and time blocks (unavailable periods) for the appointments system. It enables providers to set their regular working schedules, mark unavailable periods (like vacations or breaks), and allows customers to check available time slots for booking.

## Database

### Tables and Schema

#### WorkingHours Table
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | UUID | Primary Key | Working hours identifier |
| `TenantId` | UUID | Required, Foreign Key | Reference to tenant |
| `Day` | INTEGER | Required | Day of week (0=Sunday to 6=Saturday) |
| `StartTime` | TIME | Required | Start time for work day |
| `EndTime` | TIME | Required | End time for work day |
| `MaxConcurrentBookings` | INTEGER | Required | Maximum concurrent bookings (default: 1) |
| `CreatedAt` | TIMESTAMPTZ | Required | Creation timestamp |
| `UpdatedAt` | TIMESTAMPTZ | Required | Last update timestamp |

#### TimeBlocks Table
| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | UUID | Primary Key | Time block identifier |
| `TenantId` | UUID | Required, Foreign Key | Reference to tenant |
| `StartDateTime` | TIMESTAMPTZ | Required | Start date and time of unavailable period |
| `EndDateTime` | TIMESTAMPTZ | Required | End date and time of unavailable period |
| `Type` | VARCHAR(20) | Required | Type: Vacation, Break, Custom |
| `Reason` | VARCHAR(500) | Nullable | Optional reason for time block |
| `RecurrenceId` | UUID | Nullable | ID for recurring time blocks group |
| `CreatedAt` | TIMESTAMPTZ | Required | Creation timestamp |
| `UpdatedAt` | TIMESTAMPTZ | Required | Last update timestamp |

#### Tenants Table
**NOTE:** This table is replicated from the Users service. TODO: Syncronize it with kafka/rabbitMQ

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | UUID | Primary Key | Tenant identifier |
| `BusinessName` | VARCHAR(255) | Required | Business name |
| `Email` | VARCHAR(255) | Nullable | Business email |
| `Phone` | VARCHAR(50) | Nullable | Business phone |
| `Address` | VARCHAR(500) | Nullable | Business address |
| `TimeZone` | VARCHAR(50) | Nullable | Time zone for the tenant |
| `BufferBeforeMinutes` | INTEGER | Required | Buffer time before appointments (default: 0) |
| `BufferAfterMinutes` | INTEGER | Required | Buffer time after appointments (default: 0) |
| `CreatedAt` | TIMESTAMPTZ | Required | Creation timestamp |
| `UpdatedAt` | TIMESTAMPTZ | Required | Last update timestamp |

#### Bookings Table
**NOTE:** This table is replicated from the Boooking service. TODO: Syncronize it with kafka/rabbitMQ

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | UUID | Primary Key | Booking identifier |
| `TenantId` | UUID | Required, Foreign Key | Reference to tenant |
| `CustomerId` | UUID | Required | Customer who made the booking |
| `StartDateTime` | TIMESTAMPTZ | Required | Start time of booking |
| `EndDateTime` | TIMESTAMPTZ | Required | End time of booking |
| `BookingStatus` | INTEGER | Required | Status: Pending, Confirmed, Completed, Cancelled |
| `CreatedAt` | TIMESTAMPTZ | Required | Creation timestamp |
| `UpdatedAt` | TIMESTAMPTZ | Required | Last update timestamp |

### Database Relationships
1. **WorkingHours → Tenants:** Many-to-one via `TenantId` (working hours belong to one tenant)
2. **TimeBlocks → Tenants:** Many-to-one via `TenantId` (time blocks belong to one tenant)
3. **Bookings → Tenants:** Many-to-one via `TenantId` (bookings belong to one tenant)
4. **TimeBlocks:** Grouped by `RecurrenceId` for recurring patterns

### Foreign Key Constraints
- `FK_WorkingHours_Tenants_TenantId` → `Tenants(Id)` (ON DELETE CASCADE)
- `FK_TimeBlocks_Tenants_TenantId` → `Tenants(Id)` (ON DELETE CASCADE)
- `FK_Bookings_Tenants_TenantId` → `Tenants(Id)` (ON DELETE CASCADE)

## API Endpoints

### Availability Endpoints (`/api/availability`)

#### Get Available Slots
```http
GET /api/availability/slots?tenantId={guid}&startDate={datetime}&endDate={datetime}
Authorization: Bearer <token>
```
**Authentication:** Required (JWT token)
**Description:** Get available time slots for booking within a date range

**Remarks:**
- **CUSTOMERS:** Must provide tenantId query parameter. Can check availability for any tenant.
- **PROVIDERS:** Cannot provide tenantId parameter. Can only check availability for their own tenant.
- Date range cannot exceed 1 month

**Parameters:**
- `tenantId` (GUID, required for customers, forbidden for providers)
- `startDate` (DateTime, required) - Start date for availability check (format: 2026-01-01Z)
- `endDate` (DateTime, required) - End date for availability check (format: 2026-01-01Z)

**Response:**
```json
{
  "availableRanges": [
    {
      "start": "2025-12-10T09:00:00Z",
      "end": "2025-12-10T10:00:00Z"
    },
    {
      "start": "2025-12-10T10:30:00Z",
      "end": "2025-12-10T11:30:00Z"
    }
  ]
}
```

### Working Hours Endpoints (`/api/availability`)

#### Get Working Hours
```http
GET /api/availability/working-hours?tenantId={guid}&day={integer}
Authorization: Bearer <token>
```
**Authentication:** Required (JWT token)
**Description:** Get working hours for a tenant

**Remarks:**
- **CUSTOMERS:** Must provide tenantId query parameter. Can view working hours for any tenant.
- **PROVIDERS:** Cannot provide tenantId parameter. Can only view their own working hours.

**Parameters:**
- `tenantId` (GUID, optional) - Tenant ID (required for customers, forbidden for providers)
- `day` (Integer, optional) - Day of week to filter by (0=Sunday to 6=Saturday)

**Response:**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "tenantId": "456e7890-e89b-12d3-a456-426614174001",
    "day": 1,
    "startTime": "09:00:00",
    "endTime": "17:00:00",
    "maxConcurrentBookings": 1,
    "createdAt": "2025-12-09T10:00:00Z",
    "updatedAt": "2025-12-09T11:00:00Z"
  }
]
```

#### Create Working Hours
```http
POST /api/availability/working-hours
Content-Type: application/json
Authorization: Bearer <token>
```
**Authentication:** Required (JWT token)
**Description:** Create working hours for a specific day

**Remarks:**
- **PROVIDERS ONLY:** Can only create working hours for their own tenant
- Cannot create working hours if they already exist for the day
- Use the bulk endpoint to replace existing schedules

**Request Body:**
```json
{
  "day": 1,
  "startTime": "09:00:00",
  "endTime": "17:00:00",
  "maxConcurrentBookings": 1
}
```

**Response:** Created working hours with location header

#### Create Weekly Schedule (Bulk)
```http
POST /api/availability/working-hours/batch
Content-Type: application/json
Authorization: Bearer <token>
```
**Authentication:** Required (JWT token)
**Description:** Create weekly schedule in bulk

**Remarks:**
- **PROVIDERS ONLY:** Replaces all existing working hours with new schedule
- Allows setting work-free days
- Each schedule entry can apply to multiple days

**Request Body:**
```json
{
  "schedule": [
    {
      "days": [1, 2, 3, 4, 5],
      "startTime": "09:00:00",
      "endTime": "17:00:00",
      "isWorkFree": false,
      "maxConcurrentBookings": 1
    },
    {
      "days": [0, 6],
      "isWorkFree": true
    }
  ]
}
```

**Response:**
```json
{
  "message": "Weekly schedule created successfully",
  "createdCount": 5,
  "createdDays": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"],
  "freeDays": ["Saturday", "Sunday"]
}
```

**Note on Day Format:**
All day values use numeric format: 0=Sunday, 1=Monday, 2=Tuesday, 3=Wednesday, 4=Thursday, 5=Friday, 6=Saturday
#### Delete Working Hours
```http
DELETE /api/availability/working-hours/{id}
Authorization: Bearer <token>
```
**Authentication:** Required (JWT token)
**Description:** Delete working hours

**Remarks:**
- **PROVIDERS ONLY:** Can only delete working hours belonging to their tenant
- Working hours must exist before deletion

**Parameters:**
- `id` (GUID, required) - Working hours ID

**Response:** `204 No Content`

#### Reset Buffer Settings
```http
DELETE /api/availability/tenant-settings/buffer
Authorization: Bearer <token>
```
**Authentication:** Required (JWT token)
**Description:** Reset buffer settings to default values (0 minutes)

**Remarks:**
- **PROVIDERS ONLY:** Resets both bufferBefore and bufferAfter to 0
- This removes any buffer time between appointments

**Response:** `204 No Content`

### Time Blocks Endpoints (`/api/availability`)

#### Get Time Blocks
```http
GET /api/availability/time-blocks?offset={integer}&limit={integer}&startDate={datetime}&endDate={datetime}
Authorization: Bearer <token>
```
**Authentication:** Required (JWT token)
**Description:** Get time blocks (unavailable periods) for the provider's tenant

**Remarks:**
- **PROVIDERS ONLY:** Can only access their own time blocks
- Supports pagination
- Optional date range filtering

**Parameters:**
- `offset` (Integer, optional) - Number of items to skip (default: 0)
- `limit` (Integer, optional) - Number of items to return (default: 50)
- `startDate` (DateTime, optional) - Start date to filter time blocks (format: 2026-01-01Z)
- `endDate` (DateTime, optional) - End date to filter time blocks (format: 2026-01-01Z)

**Response:**
```json
{
  "offset": 0,
  "limit": 50,
  "totalCount": 2,
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "tenantId": "456e7890-e89b-12d3-a456-426614174001",
      "startDateTime": "2025-12-25T09:00:00Z",
      "endDateTime": "2025-12-25T17:00:00Z",
      "type": "Vacation",
      "reason": "Christmas holidays",
      "isRecurring": false,
      "createdAt": "2025-12-09T10:00:00Z",
      "updatedAt": "2025-12-09T11:00:00Z"
    }
  ]
}
```

#### Get Time Block by ID
```http
GET /api/availability/time-blocks/{id}
Authorization: Bearer <token>
```
**Authentication:** Required (JWT token)
**Description:** Get a specific time block by ID

**Remarks:**
- **PROVIDERS ONLY:** Can only access time blocks belonging to their tenant

**Parameters:**
- `id` (GUID, required) - Time block ID

**Response:** Time block details

#### Create Time Block
```http
POST /api/availability/time-blocks
Content-Type: application/json
Authorization: Bearer <token>
```
**Authentication:** Required (JWT token)
**Description:** Create a new time block (unavailable period)

**Remarks:**
- **PROVIDERS ONLY:** Can only create time blocks for their own tenant
- Supports recurring patterns:
  - **Daily**: Every day or every N days (use `interval`)
  - **Weekly**: Every N weeks on specific days (use `daysOfWeek` where 0=Sunday)
  - **Monthly**: Every N months on specific days (use `daysOfMonth`, negative values for from end)
- Must provide an end condition: either `endDate` (exclusive) or `maxOccurrences`
- Cannot create time blocks in the past
- Types: Vacation, Break, Custom

**Request Body:**
```json
{
  "startDateTime": "2025-12-25T09:00:00Z",
  "endDateTime": "2025-12-25T17:00:00Z",
  "type": "Vacation",
  "reason": "Christmas holidays",
  "recurrencePattern": {
    "frequency": "Weekly",
    "interval": 1,
    "daysOfWeek": [1, 3, 5],
    "endDate": "2025-12-31T23:59:59Z"
  }
}
```

**Recurrence Pattern Examples:**

1. **Daily Pattern (every 2 days):**
```json
{
  "frequency": "Daily",
  "interval": 2,
  "endDate": "2025-12-31T23:59:59Z"
}
```

2. **Weekly Pattern (Mondays and Fridays):**
```json
{
  "frequency": "Weekly",
  "interval": 1,
  "daysOfWeek": [1, 5],
  "maxOccurrences": 10
}
```

3. **Monthly Pattern (15th and last day of each month):**
```json
{
  "frequency": "Monthly",
  "interval": 1,
  "daysOfMonth": [15, -1],
  "endDate": "2026-06-30T23:59:59Z"
}
```

**Response:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "tenantId": "456e7890-e89b-12d3-a456-426614174001",
  "startDateTime": "2025-12-25T09:00:00Z",
  "endDateTime": "2025-12-25T17:00:00Z",
  "type": "Vacation",
  "reason": "Christmas holidays",
  "isRecurring": true,
  "createdAt": "2025-12-09T10:00:00Z",
  "updatedAt": "2025-12-09T11:00:00Z",
  "totalCreated": 2
}
```

#### Update Time Block (Partial)
```http
PATCH /api/availability/time-blocks/{id}?editPattern={boolean}
Content-Type: application/json
Authorization: Bearer <token>
```
**Authentication:** Required (JWT token)
**Description:** Update a time block (partial update)

**Remarks:**
- **PROVIDERS ONLY:** Can only update time blocks belonging to their tenant
- For recurring blocks, use editPattern=true to update all occurrences
- editPattern=false updates only the specific instance

**Parameters:**
- `id` (GUID, required) - Time block ID
- `editPattern` (Boolean, optional) - Whether to edit the entire recurring pattern (default: false)

**Request Body:**
```json
{
  "startDateTime": "2025-12-25T10:00:00Z",
  "endDateTime": "2025-12-25T16:00:00Z",
  "reason": "Updated Christmas hours"
}
```

**Response:** Updated time block

#### Delete Time Block
```http
DELETE /api/availability/time-blocks/{id}?deletePattern={boolean}
Authorization: Bearer <token>
```
**Authentication:** Required (JWT token)
**Description:** Delete a time block

**Remarks:**
- **PROVIDERS ONLY:** Can only delete time blocks belonging to their tenant
- For recurring blocks, use deletePattern=true to delete all occurrences
- deletePattern=false deletes only the specific instance

**Parameters:**
- `id` (GUID, required) - Time block ID
- `deletePattern` (Boolean, optional) - Whether to delete the entire recurring pattern (default: false)

**Response:** `204 No Content`

#### Delete Time Blocks by Date Range (Bulk)
```http
DELETE /api/availability/time-blocks/range
Content-Type: application/json
Authorization: Bearer <token>
```
**Authentication:** Required (JWT token)
**Description:** Delete multiple time blocks within a date range

**Remarks:**
- **PROVIDERS ONLY:** Can only delete time blocks belonging to their tenant
- Deletes all time blocks that fall within the specified date range
- Useful for clearing vacation periods or bulk operations

**Request Body:**
```json
{
  "startDate": "2025-12-24T00:00:00Z",
  "endDate": "2025-12-31T23:59:59Z"
}
```

**Response:**
```json
{
  "message": "Time blocks deleted successfully",
  "deletedCount": 5
}
```

### Tenant Settings Endpoints (`/api/availability`)

#### Get Buffer Settings
```http
GET /api/availability/tenant-settings/buffer
Authorization: Bearer <token>
```
**Authentication:** Required (JWT token)
**Description:** Get buffer settings for the provider's tenant

**Remarks:**
- **PROVIDERS ONLY:** Buffer time is added before and after appointments
- Used to prevent back-to-back bookings
- Default values are 0 minutes

**Response:**
```json
{
  "bufferBeforeMinutes": 15,
  "bufferAfterMinutes": 15
}
```

#### Update Buffer Settings
```http
PATCH /api/availability/tenant-settings/buffer
Content-Type: application/json
Authorization: Bearer <token>
```
**Authentication:** Required (JWT token)
**Description:** Update buffer settings for the provider's tenant

**Remarks:**
- **PROVIDERS ONLY:** Buffer time is added before and after appointments
- Used to prevent back-to-back bookings
- Helps with preparation time between appointments

**Request Body:**
```json
{
  "bufferBeforeMinutes": 15,
  "bufferAfterMinutes": 30
}
```

**Response:** Updated buffer settings

## gRPC Service

The Availability Service exposes a gRPC endpoint for time slot availability checking. This is primarily used by the Booking Service to validate booking requests.

### gRPC Service Definition

### gRPC Endpoint

- **URL:** Configured in `appsettings.json` or via environment variable
- **Default URL:** `http://localhost:5003` (development)
- **Port:** The gRPC service is mapped to port 5003 by default

### Request Validation

The gRPC service performs the following validations:

1. **Required Fields:**
   - `tenant_id` must be a valid GUID
   - `service_id` must be a valid GUID
   - `start_time` and `end_time` must be provided

2. **Time Validation:**
   - `start_time` must be before `end_time`
   - Times are converted to UTC for consistency

3. **Business Logic:**
   - Checks against working hours for the tenant
   - Verifies no overlapping time blocks (unavailable periods)
   - Ensures no existing bookings conflict with the slot
   - Respects tenant's buffer time settings
   - Optionally excludes a specific booking ID (useful for rescheduling)

### Response Format

```json
{
  "isAvailable": true/false,
  "conflicts": [
    {
      "type": "WorkingHours",
      "overlapStart": "2025-12-10T18:00:00Z",
      "overlapEnd": "2025-12-10T19:00:00Z"
    }
  ]
}
```

- When `isAvailable = true`: The `conflicts` list will be empty
- When `isAvailable = false`: The `conflicts` list contains all conflicts preventing the booking
- Each conflict includes:
  - `type`: The type of conflict (TimeBlock, WorkingHours, Booking, BufferTime, or Unspecified)
  - `overlapStart`: When the conflict begins
  - `overlapEnd`: When the conflict ends

### Error Handling

The gRPC service handles errors gracefully:
- Invalid GUIDs return `isAvailable = false` with an Unspecified conflict
- Invalid time ranges return `isAvailable = false` with an Unspecified conflict
- Unexpected exceptions return `isAvailable = false` with an Unspecified conflict
- All responses are logged for debugging purposes

### Integration Example

**Booking Service Client:**
```csharp
var client = new AvailabilityService.AvailabilityService.AvailabilityServiceClient(channel);
var request = new TimeSlotRequest
{
    TenantId = "456e7890-e89b-12d3-a456-426614174001",
    ServiceId = "789e0123-e89b-12d3-a456-426614174000",
    StartTime = Timestamp.FromDateTime(DateTime.UtcNow.AddHours(1)),
    EndTime = Timestamp.FromDateTime(DateTime.UtcNow.AddHours(2))
};

var response = await client.CheckTimeSlotAvailabilityAsync(request);
if (response.IsAvailable)
{
    // Proceed with booking creation
}
else
{
    // Build detailed error message from conflicts
    var conflictMessages = response.Conflicts
        .Select(c => $"{c.Type}: {c.OverlapStart:HH:mm} - {c.OverlapEnd:HH:mm}");

    var errorMessage = $"The requested time slot is not available due to: {string.Join(", ", conflictMessages)}";

    // Show error to user
    Console.WriteLine(errorMessage);
}
```

## Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `DATABASE_CONNECTION_STRING` | Yes | PostgreSQL connection string |
| `JWT_SECRET_KEY` | Yes | JWT signing key (minimum 128 bits) |
| `ASPNETCORE_ENVIRONMENT` | No | Environment (Development/Production) |
| `GRPC_URL` | No | gRPC service URL (default: http://localhost:5003) |
| `KAFKA__BOOTSTRAPSERVERS` | Yes | Kafka bootstrap servers |
| `KAFKA__TENANTEVENTSTOPIC` | Yes | Tenant events topic (consumer) |
| `KAFKA__BOOKINGEVENTSTOPIC` | Yes | Booking events topic (consumer) |
| `KAFKA__CONSUMERGROUPID` | Yes | Kafka consumer group ID |
| `KAFKA__ENABLEAUTOCOMMIT` | Yes | Kafka auto-commit setting |
| `KAFKA__AUTOOFFSETRESET` | Yes | Kafka auto offset reset |

## Health Checks

- `GET /health` - Complete health check including database
- `GET /health/live` - Basic service liveness check
- `GET /health/ready` - Readiness check for dependencies

## Kafka Events

The Availability Service acts as a **Kafka Consumer** only (for tenant and booking events).

### Consumed Events

| Event Type | Topic | Handler | Description |
|------------|-------|---------|-------------|
| `TenantCreatedEvent` | `tenant-events` | `TenantEventService` | Creates tenant in local database |
| `TenantUpdatedEvent` | `tenant-events` | `TenantEventService` | Updates tenant in local database |
| `BookingCreatedEvent` | `booking-events` | `BookingEventService` | Creates booking in local database |
| `BookingCancelledEvent` | `booking-events` | `BookingEventService` | Updates booking status to Cancelled in local database |

### Consumer Configuration

- **Auto Commit**: `false` (manual offset management)
- **Offset Reset**: `earliest` (read from beginning)
- **Consumer Groups**:
  - `availability-service-tenant-events` (for tenant events)
  - `availability-service-booking-events` (for booking events)