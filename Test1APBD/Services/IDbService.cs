using Test1APBD.Models.DTOs;

namespace Test1APBD.Services;

public interface IDbService
{
    Task<AppointmentDetailsDto> GetAppointmentByIdAsync(int appointmentId);
    Task AddAppointmentAsync(CreateAppointmentDto appointmentRequest);
}