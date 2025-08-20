using System;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;

namespace whatsapp_ollama_bot.Services
{
    /// <summary>
    /// Service for interacting with Google Calendar API.
    /// It authenticates using a service account credential and provides methods to create events.
    /// </summary>
    public class GoogleCalendarService
    {
        private readonly CalendarService _service;
        private readonly string _calendarId;

        public GoogleCalendarService(IConfiguration configuration)
        {
            // Read configuration values for Google Calendar API.
            var credentialsPath = configuration.GetValue<string>("GoogleCalendar:CredentialsPath");
            var applicationName = configuration.GetValue<string>("GoogleCalendar:ApplicationName");
            _calendarId = configuration.GetValue<string>("GoogleCalendar:CalendarId") ?? "primary";

            if (string.IsNullOrWhiteSpace(credentialsPath))
            {
                throw new ArgumentException("GoogleCalendar:CredentialsPath is not configured.");
            }

            if (string.IsNullOrWhiteSpace(applicationName))
            {
                throw new ArgumentException("GoogleCalendar:ApplicationName is not configured.");
            }

            // Load the service account credentials and create the Calendar service.
            var credential = GoogleCredential.FromFile(credentialsPath)
                .CreateScoped(CalendarService.Scope.Calendar, CalendarService.Scope.CalendarEvents);
            _service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName
            });
        }

        /// <summary>
        /// Creates an event on the configured Google Calendar.
        /// </summary>
        /// <param name="summary">Title or summary of the event.</param>
        /// <param name="description">Detailed description of the event.</param>
        /// <param name="startTime">Start time of the event (local).</param>
        /// <param name="endTime">End time of the event (local).</param>
        /// <param name="timeZone">Time zone identifier (e.g., "America/Sao_Paulo").</param>
        /// <returns>The created Event resource.</returns>
        public async Task<Event> CreateEventAsync(string summary, string description, DateTime startTime, DateTime endTime, string timeZone = "America/Sao_Paulo")
        {
            var newEvent = new Event
            {
                Summary = summary,
                Description = description,
                Start = new EventDateTime
                {
                    DateTime = startTime,
                    TimeZone = timeZone
                },
                End = new EventDateTime
                {
                    DateTime = endTime,
                    TimeZone = timeZone
                }
            };

            var insertRequest = _service.Events.Insert(newEvent, _calendarId);
            return await insertRequest.ExecuteAsync().ConfigureAwait(false);
        }
    }
}
