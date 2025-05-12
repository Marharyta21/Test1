using Microsoft.AspNetCore.Mvc;
using Test1APBD.Exceptions;
using Test1APBD.Models.DTOs;
using Test1APBD.Services;

namespace Test1APBD.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly IDbService _dbService;
        
        public AppointmentsController(IDbService dbService)
        {
            _dbService = dbService;
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAppointment(int id)
        {
            try
            {
                var appointment = await _dbService.GetAppointmentByIdAsync(id);
                return Ok(appointment);
            }
            catch (NotFoundException e)
            {
                return NotFound(e.Message);
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> CreateAppointment(CreateAppointmentDto appointmentRequest)
        {
            if (appointmentRequest.Services == null || !appointmentRequest.Services.Any())
            {
                return BadRequest("At least one service is required.");
            }
            
            try
            {
                await _dbService.AddAppointmentAsync(appointmentRequest);
                return CreatedAtAction(nameof(GetAppointment), new { id = appointmentRequest.AppointmentId }, appointmentRequest);
            }
            catch (NotFoundException e)
            {
                return NotFound(e.Message);
            }
            catch (ConflictException e)
            {
                return Conflict(e.Message);
            }
        }
    }
}