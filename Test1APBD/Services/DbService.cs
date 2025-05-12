using System.Data.Common;
using Microsoft.Data.SqlClient;
using Test1APBD.Exceptions;
using Test1APBD.Models.DTOs;

namespace Test1APBD.Services
{
    public class DbService : IDbService 
    {
        private readonly string _connectionString;
        
        public DbService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Default") ?? string.Empty;
        }
        
        public async Task<AppointmentDetailsDto> GetAppointmentByIdAsync(int appointmentId)
        {
            var query = @"
                SELECT a.date, 
                       p.first_name, p.last_name, p.date_of_birth,
                       d.doctor_id, d.PWZ,
                       s.name, as_service.service_fee
                FROM Appointment a
                JOIN Patient p ON a.patient_id = p.patient_id
                JOIN Doctor d ON a.doctor_id = d.doctor_id
                JOIN Appointment_Service as_service ON a.appointment_id = as_service.appointment_id
                JOIN Service s ON as_service.service_id = s.service_id
                WHERE a.appointment_id = @appointmentId";
            
            await using SqlConnection connection = new SqlConnection(_connectionString);
            await using SqlCommand command = new SqlCommand();
            
            command.Connection = connection;
            command.CommandText = query;
            command.Parameters.AddWithValue("@appointmentId", appointmentId);
            
            await connection.OpenAsync();
            var reader = await command.ExecuteReaderAsync();
            
            AppointmentDetailsDto? appointmentDetails = null;
            
            while (await reader.ReadAsync())
            {
                if (appointmentDetails == null)
                {
                    appointmentDetails = new AppointmentDetailsDto
                    {
                        Date = reader.GetDateTime(0),
                        Patient = new PatientDto
                        {
                            FirstName = reader.GetString(1),
                            LastName = reader.GetString(2),
                            DateOfBirth = reader.GetDateTime(3)
                        },
                        Doctor = new DoctorDto
                        {
                            DoctorId = reader.GetInt32(4),
                            PWZ = reader.GetString(5)
                        },
                        AppointmentServices = new List<AppointmentServiceDto>()
                    };
                }
                
                appointmentDetails.AppointmentServices.Add(new AppointmentServiceDto
                {
                    Name = reader.GetString(6),
                    ServiceFee = reader.GetDecimal(7)
                });
            }
            
            if (appointmentDetails == null)
            {
                throw new NotFoundException($"Appointment with ID {appointmentId} not found.");
            }
            
            return appointmentDetails;
        }
        
        public async Task AddAppointmentAsync(CreateAppointmentDto appointmentRequest)
        {
            await using SqlConnection connection = new SqlConnection(_connectionString);
            await using SqlCommand command = new SqlCommand();
            
            command.Connection = connection;
            await connection.OpenAsync();
            
            DbTransaction transaction = await connection.BeginTransactionAsync();
            command.Transaction = transaction as SqlTransaction;
            
            try
            {
                command.CommandText = "SELECT 1 FROM Appointment WHERE appointment_id = @AppointmentId";
                command.Parameters.AddWithValue("@AppointmentId", appointmentRequest.AppointmentId);
                
                var existingAppointment = await command.ExecuteScalarAsync();
                if (existingAppointment != null)
                {
                    throw new ConflictException($"Appointment with ID {appointmentRequest.AppointmentId} already exists.");
                }
                
                command.Parameters.Clear();
                command.CommandText = "SELECT 1 FROM Patient WHERE patient_id = @PatientId";
                command.Parameters.AddWithValue("@PatientId", appointmentRequest.PatientId);
                
                var patientExists = await command.ExecuteScalarAsync();
                if (patientExists == null)
                {
                    throw new NotFoundException($"Patient with ID {appointmentRequest.PatientId} not found.");
                }
                
                command.Parameters.Clear();
                command.CommandText = "SELECT doctor_id FROM Doctor WHERE PWZ = @PWZ";
                command.Parameters.AddWithValue("@PWZ", appointmentRequest.PWZ);
                
                var doctorId = await command.ExecuteScalarAsync();
                if (doctorId == null)
                {
                    throw new NotFoundException($"Doctor with PWZ {appointmentRequest.PWZ} not found.");
                }
                
                command.Parameters.Clear();
                command.CommandText = @"
                    INSERT INTO Appointment (appointment_id, patient_id, doctor_id, date)
                    VALUES (@AppointmentId, @PatientId, @DoctorId, @Date)";
                
                command.Parameters.AddWithValue("@AppointmentId", appointmentRequest.AppointmentId);
                command.Parameters.AddWithValue("@PatientId", appointmentRequest.PatientId);
                command.Parameters.AddWithValue("@DoctorId", doctorId);
                command.Parameters.AddWithValue("@Date", DateTime.Now);
                
                await command.ExecuteNonQueryAsync();
                
                foreach (var service in appointmentRequest.Services)
                {
                    command.Parameters.Clear();
                    command.CommandText = "SELECT service_id FROM Service WHERE name = @ServiceName";
                    command.Parameters.AddWithValue("@ServiceName", service.ServiceName);
                    
                    var serviceId = await command.ExecuteScalarAsync();
                    if (serviceId == null)
                    {
                        throw new NotFoundException($"Service with name '{service.ServiceName}' not found.");
                    }
                    
                    command.Parameters.Clear();
                    command.CommandText = @"
                        INSERT INTO Appointment_Service (appointment_id, service_id, service_fee)
                        VALUES (@AppointmentId, @ServiceId, @ServiceFee)";
                    
                    command.Parameters.AddWithValue("@AppointmentId", appointmentRequest.AppointmentId);
                    command.Parameters.AddWithValue("@ServiceId", serviceId);
                    command.Parameters.AddWithValue("@ServiceFee", service.ServiceFee);
                    
                    await command.ExecuteNonQueryAsync();
                }
                
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}