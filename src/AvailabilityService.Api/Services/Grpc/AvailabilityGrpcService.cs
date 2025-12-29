using Grpc.Core;
using AvailabilityService.Api.Services.Interfaces;
using AvailabilityService.Api.Models.Internal;
using Google.Protobuf.WellKnownTypes;

namespace AvailabilityService.Api.Services.Grpc;

public class AvailabilityGrpcService(
    ILogger<AvailabilityGrpcService> logger,
    IAvailabilityService availabilityService)
    : Availability.AvailabilityService.AvailabilityServiceBase
{
    public override async Task<Availability.TimeSlotResponse> CheckTimeSlotAvailability(
        Availability.TimeSlotRequest request,
        ServerCallContext context)
    {
        try
        {
            logger.LogInformation(
                "Checking time slot availability for tenant {TenantId}, service {ServiceId}, time {StartTime} - {EndTime}",
                request.TenantId,
                request.ServiceId,
                request.StartTime.ToDateTime(),
                request.EndTime.ToDateTime());

            if (!Guid.TryParse(request.TenantId, out var tenantId) ||
                !Guid.TryParse(request.ServiceId, out var serviceId))
            {
                return new Availability.TimeSlotResponse
                {
                    IsAvailable = false,
                    Conflicts = {
                        new Availability.ConflictInfo
                        {
                            Type = Availability.ConflictType.Unspecified,
                            OverlapStart = Timestamp.FromDateTime(DateTime.UtcNow),
                            OverlapEnd = Timestamp.FromDateTime(DateTime.UtcNow)
                        }
                    }
                };
            }

            var startTime = request.StartTime.ToDateTime();
            var endTime = request.EndTime.ToDateTime();

            if (startTime >= endTime)
            {
                return new Availability.TimeSlotResponse
                {
                    IsAvailable = false,
                    Conflicts = {
                        new Availability.ConflictInfo
                        {
                            Type = Availability.ConflictType.Unspecified,
                            OverlapStart = Timestamp.FromDateTime(startTime.ToUniversalTime()),
                            OverlapEnd = Timestamp.FromDateTime(endTime.ToUniversalTime())
                        }
                    }
                };
            }

            var availabilityResult = await availabilityService.IsTimeSlotAvailableAsync(
                tenantId,
                serviceId,
                startTime,
                endTime);

            logger.LogInformation(
                "Time slot availability check completed for tenant {TenantId}, service {ServiceId}: {IsAvailable}, Conflicts: {ConflictCount}",
                request.TenantId,
                request.ServiceId,
                availabilityResult.IsAvailable,
                availabilityResult.Conflicts.Count);

            var response = new Availability.TimeSlotResponse
            {
                IsAvailable = availabilityResult.IsAvailable
            };

            foreach (var conflict in availabilityResult.Conflicts)
            {
                response.Conflicts.Add(new Availability.ConflictInfo
                {
                    Type = MapConflictType(conflict.Type),
                    OverlapStart = Timestamp.FromDateTime(conflict.OverlapStart.ToUniversalTime()),
                    OverlapEnd = Timestamp.FromDateTime(conflict.OverlapEnd.ToUniversalTime())
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking time slot availability");

            return new Availability.TimeSlotResponse
            {
                IsAvailable = false,
                Conflicts = {
                    new Availability.ConflictInfo
                    {
                        Type = Availability.ConflictType.Unspecified,
                        OverlapStart = Timestamp.FromDateTime(DateTime.UtcNow),
                        OverlapEnd = Timestamp.FromDateTime(DateTime.UtcNow)
                    }
                }
            };
        }
    }

    private static Availability.ConflictType MapConflictType(ConflictType type)
    {
        return type switch
        {
            ConflictType.TimeBlock => Availability.ConflictType.TimeBlock,
            ConflictType.WorkingHours => Availability.ConflictType.WorkingHours,
            ConflictType.Booking => Availability.ConflictType.Booking,
            ConflictType.BufferTime => Availability.ConflictType.BufferTime,
            _ => Availability.ConflictType.Unspecified
        };
    }
}