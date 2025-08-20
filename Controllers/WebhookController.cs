using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using WhatsappBusiness.CloudApi;
using WhatsappBusiness.CloudApi.Messages.Requests;
using WhatsappBusiness.CloudApi.Models.Messages;
using whatsapp_ollama_bot.Services;

namespace whatsapp_ollama_bot.Controllers
{
    /// <summary>
    /// Controller that exposes a webhook endpoint for Meta's WhatsApp Business Cloud API.
    /// It verifies the webhook when configured by Meta and processes incoming messages by
    /// forwarding them to a local language model and replying with the generated response.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class WebhookController : ControllerBase
    {
        private readonly IWhatsAppBusinessClient _whatsAppClient;
        private readonly OllamaClient _ollamaClient;
        private readonly string _verifyToken;

        public WebhookController(
            IWhatsAppBusinessClient whatsAppClient,
            OllamaClient ollamaClient,
            IConfiguration configuration)
        {
            _whatsAppClient = whatsAppClient;
            _ollamaClient = ollamaClient;
            _verifyToken = configuration.GetValue<string>("WhatsAppBusinessCloudApiConfiguration:VerifyToken") ?? string.Empty;
        }

        /// <summary>
        /// Verification endpoint invoked by Meta to confirm your webhook URL.
        /// Returns the challenge value if the verify token matches.
        /// </summary>
        [HttpGet]
        public IActionResult Verify(
            [FromQuery(Name = "hub.mode")] string mode,
            [FromQuery(Name = "hub.challenge")] int challenge,
            [FromQuery(Name = "hub.verify_token")] string verifyToken)
        {
            // Meta sends 'subscribe' when verifying the webhook. The challenge must be returned
            // only if the token matches the one you configured in appsettings.json.
            if (mode == "subscribe" && verifyToken == _verifyToken)
            {
                return Ok(challenge);
            }
            return BadRequest("Invalid verification token");
        }

        /// <summary>
        /// Handles incoming message events from WhatsApp via webhook.
        /// Extracts the sender and message text, forwards it to the local LLM,
        /// and sends the AI-generated reply back to the user.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Receive([FromBody] object payload)
        {
            try
            {
                // Parse the incoming JSON payload into a JObject for easier access.
                var json = JObject.Parse(payload?.ToString() ?? "{}");

                // Navigate to the message text and sender phone number.
                var messages = json["entry"]?[0]?["changes"]?[0]?["value"]?["messages"];
                if (messages != null && messages.HasValues)
                {
                    var message = messages[0];
                    var from = message["from"]?.ToString();
                    var body = message["text"]?["body"]?.ToString();

                    if (!string.IsNullOrWhiteSpace(from) && !string.IsNullOrWhiteSpace(body))
                    {
                        // Ask the local Ollama model for a response.
                        var reply = await _ollamaClient.GenerateResponseAsync(body);

                        // Build the request to send back via WhatsApp.
                        var response = new TextMessageRequest
                        {
                            To = from,
                            Text = new WhatsAppText
                            {
                                Body = reply,
                                PreviewUrl = false
                            }
                        };

                        // Send the reply to the user.
                        await _whatsAppClient.SendTextMessageAsync(response);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle accordingly in a real application.
                // For this sample we return 200 so WhatsApp doesn't retry indefinitely.
                Console.Error.WriteLine(ex);
            }

            // Always return 200 OK to acknowledge the event.
            return Ok();
        }
    }
}