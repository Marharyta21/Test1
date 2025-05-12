namespace Test1APBD.Models.DTOs;

public class AppointmentDetailsDto
{
    public DateTime Date { get; set; }
    public PatientDto Patient { get; set; }
    public DoctorDto Doctor { get; set; }
    public List<AppointmentServiceDto> AppointmentServices { get; set; } = [];
}