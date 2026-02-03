using Alfanar.MarketIntel.Application.DTOs;
using Alfanar.MarketIntel.Domain.Entities;
using Alfanar.MarketIntel.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Alfanar.MarketIntel.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactFormController : ControllerBase
{
    private readonly IContactFormSubmissionRepository _contactFormRepository;
    private readonly ILogger<ContactFormController> _logger;

    public ContactFormController(
        IContactFormSubmissionRepository contactFormRepository,
        ILogger<ContactFormController> logger)
    {
        _contactFormRepository = contactFormRepository;
        _logger = logger;
    }

    /// <summary>
    /// Submit a new contact form
    /// </summary>
    [HttpPost("submit")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitContactForm([FromBody] CreateContactFormSubmissionDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var submission = new ContactFormSubmission
            {
                Name = dto.Name,
                Email = dto.Email,
                Subject = dto.Subject,
                Message = dto.Message,
                Status = "New",
                IsRead = false,
                SubmittedAt = DateTime.UtcNow
            };

            var id = await _contactFormRepository.CreateAsync(submission);
            _logger.LogInformation($"Contact form submitted by {dto.Email} with ID {id}");

            return CreatedAtAction(nameof(GetContactFormById), new { id }, new { id, message = "Contact form submitted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error submitting contact form: {ex.Message}");
            return StatusCode(500, "An error occurred while submitting the form");
        }
    }

    /// <summary>
    /// Get all contact form submissions (for admin)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllContactForms([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var skip = (page - 1) * pageSize;
            var forms = await _contactFormRepository.GetAllAsync(skip, pageSize);
            return Ok(new { items = forms, page, pageSize, total = forms.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving contact forms: {ex.Message}");
            return StatusCode(500, "An error occurred while retrieving forms");
        }
    }

    /// <summary>
    /// Get unread contact forms
    /// </summary>
    [HttpGet("unread")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadForms()
    {
        try
        {
            var forms = await _contactFormRepository.GetUnreadAsync();
            return Ok(new { items = forms, count = forms.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving unread forms: {ex.Message}");
            return StatusCode(500, "An error occurred");
        }
    }

    /// <summary>
    /// Get contact form by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContactFormById(int id)
    {
        try
        {
            var form = await _contactFormRepository.GetByIdAsync(id);
            if (form == null)
                return NotFound($"Contact form with ID {id} not found");

            // Mark as read
            if (!form.IsRead)
            {
                form.IsRead = true;
                await _contactFormRepository.UpdateAsync(form);
            }

            return Ok(form);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving contact form: {ex.Message}");
            return StatusCode(500, "An error occurred");
        }
    }

    /// <summary>
    /// Get forms by email
    /// </summary>
    [HttpGet("email/{email}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFormsByEmail(string email)
    {
        try
        {
            var forms = await _contactFormRepository.GetByEmailAsync(email);
            return Ok(new { items = forms, count = forms.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving forms by email: {ex.Message}");
            return StatusCode(500, "An error occurred");
        }
    }

    /// <summary>
    /// Get forms by status
    /// </summary>
    [HttpGet("status/{status}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFormsByStatus(string status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var skip = (page - 1) * pageSize;
            var forms = await _contactFormRepository.GetByStatusAsync(status, skip, pageSize);
            return Ok(new { items = forms, page, pageSize, status });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error retrieving forms by status: {ex.Message}");
            return StatusCode(500, "An error occurred");
        }
    }

    /// <summary>
    /// Respond to a contact form
    /// </summary>
    [HttpPut("{id}/respond")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RespondToForm(int id, [FromBody] dynamic request)
    {
        try
        {
            var form = await _contactFormRepository.GetByIdAsync(id);
            if (form == null)
                return NotFound($"Contact form with ID {id} not found");

            form.ResponseMessage = request.responseMessage;
            form.RespondedAt = DateTime.UtcNow;
            form.RespondedBy = request.respondedBy ?? "System";
            form.Status = "Resolved";

            await _contactFormRepository.UpdateAsync(form);
            _logger.LogInformation($"Response sent for contact form {id}");

            return Ok(new { message = "Response saved successfully", form });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error responding to form: {ex.Message}");
            return StatusCode(500, "An error occurred");
        }
    }

    /// <summary>
    /// Update form status
    /// </summary>
    [HttpPut("{id}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFormStatus(int id, [FromBody] dynamic request)
    {
        try
        {
            var form = await _contactFormRepository.GetByIdAsync(id);
            if (form == null)
                return NotFound();

            form.Status = request.status;
            await _contactFormRepository.UpdateAsync(form);

            return Ok(new { message = "Status updated successfully", status = form.Status });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating form status: {ex.Message}");
            return StatusCode(500, "An error occurred");
        }
    }
}
