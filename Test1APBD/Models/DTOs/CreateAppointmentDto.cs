namespace Test1APBD.Models.DTOs;

public class CreateAppointmentDto
{
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    public string PWZ { get; set; } = string.Empty;
    public List<ServiceRequestDto> Services { get; set; } = [];
}