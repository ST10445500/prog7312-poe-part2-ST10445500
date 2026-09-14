using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using SmartX.Api.Models.Attachments;
using SmartX.Api.Services;
using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//AttachmentsController

//.....................................o0oSTART OF FILEo0o........................................//

// The API controller receives HTTP requests and sends the work to services.

namespace SmartX.Api.Controllers
{
    //handles files attached to a sensor, usually configs and photos
    [ApiController]
    [Route("api/sensors/{macAddress}/attachments")]
    public class AttachmentsController : ControllerBase
    {
        //the largest file the gateway will take, in bytes
        private const long MaxUploadBytes = 25 * 1024 * 1024;

        //room for the multipart boundary, headers and file name around the file
        private const long MultipartEnvelopeBytes = 64 * 1024;

        //the largest request body the gateway will read, file plus envelope
        private const long MaxRequestBytes = MaxUploadBytes + MultipartEnvelopeBytes;

        private readonly SensorStore _sensors;
        private readonly AttachmentStore _attachments;
        private readonly AttachmentEncryption _encryption;

        public AttachmentsController(SensorStore sensors, AttachmentStore attachments, AttachmentEncryption encryption)
        {
            _sensors = sensors;
            _attachments = attachments;
            _encryption = encryption;
        }

        //..............................................................................//

        //attaches a file to a sensor, encrypting it on the way to disk
        [HttpPost]
        [RequestSizeLimit(MaxRequestBytes)]
        public async Task<ActionResult<AttachmentSummary>> Upload(string macAddress, IFormFile? file, CancellationToken token)
        {
            var sensor = _sensors.Find(macAddress);

            if (sensor == null)
            {
                return NotFound($"No sensor registered with MAC address {macAddress}.");
            }

            // Binding gives no file when the part is missing or named something else.
            if (file == null)
            {
                return BadRequest("The request carried no file.");
            }

            if (file.Length > MaxUploadBytes)
            {
                return BadRequest($"The uploaded file was larger than the {MaxUploadBytes} byte limit.");
            }

            if (file.Length == 0)
            {
                return BadRequest("The uploaded file was empty.");
            }

            var fileName = Path.GetFileName(file.FileName);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest("The uploaded file had no name.");
            }

            var attachment = new Attachment
            {
                Id = Guid.NewGuid().ToString("N"),
                MacAddress = sensor.MacAddress,
                FileName = fileName,
                ContentType = file.ContentType,
                UploadedAt = DateTime.UtcNow,

                // A generated name stops an upload escaping the uploads folder.
                StoredFileName = $"{Guid.NewGuid():N}.bin"
            };

            var path = _attachments.PathFor(attachment.StoredFileName);

            try
            {
                using var source = file.OpenReadStream();
                using var destination = System.IO.File.Create(path);

                attachment.SizeInBytes = await _encryption.EncryptToAsync(source, destination, token);

                // Sixteen bytes of iv, then ciphertext padded to whole aes blocks.
                attachment.StoredSizeInBytes = destination.Length;
            }
            catch (Exception)
            {
                // A half written file is worse than none.
                DeleteQuietly(path);
                throw;
            }

            _attachments.Add(attachment);
            return Summarise(attachment);
        }

        //..............................................................................//

        //retrieves every file attached to a sensor
        [HttpGet]
        public ActionResult<List<AttachmentSummary>> GetAll(string macAddress)
        {
            var sensor = _sensors.Find(macAddress);

            if (sensor == null)
            {
                return NotFound($"No sensor registered with MAC address {macAddress}.");
            }

            return _attachments.GetFor(sensor.MacAddress).Select(Summarise).ToList();
        }

        //..............................................................................//

        //retrieves one attached file, decrypting it on the way out
        [HttpGet("{id}")]
        public async Task<IActionResult> Download(string macAddress, string id, CancellationToken token)
        {
            var sensor = _sensors.Find(macAddress);

            if (sensor == null)
            {
                return NotFound($"No sensor registered with MAC address {macAddress}.");
            }

            var attachment = _attachments.Find(sensor.MacAddress, id);

            if (attachment == null)
            {
                return NotFound($"{sensor.MacAddress} has no attachment with id {id}.");
            }

            var path = _attachments.PathFor(attachment.StoredFileName);

            if (!System.IO.File.Exists(path))
            {
                return NotFound("The encrypted copy of that file is no longer on disk.");
            }

            // Decrypting onto the response keeps the file out of memory.
            Response.ContentType = attachment.ContentType;
            Response.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileNameStar = attachment.FileName
            }.ToString();

            using var stored = System.IO.File.OpenRead(path);
            await _encryption.DecryptToAsync(stored, Response.Body, token);

            return new EmptyResult();
        }

        //..............................................................................//

        //removes an attached file, both what described it and the encrypted copy on disk
        [HttpDelete("{id}")]
        public ActionResult Remove(string macAddress, string id)
        {
            var sensor = _sensors.Find(macAddress);

            if (sensor == null)
            {
                return NotFound($"No sensor registered with MAC address {macAddress}.");
            }

            var removed = _attachments.Remove(sensor.MacAddress, id);

            if (removed == null)
            {
                return NotFound($"{sensor.MacAddress} has no attachment with id {id}.");
            }

            DeleteQuietly(_attachments.PathFor(removed.StoredFileName));
            return NoContent();
        }

        //..............................................................................//

        //removes a file without letting a failed cleanup hide the original problem
        private static void DeleteQuietly(string path)
        {
            try
            {
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }
            catch (IOException)
            {
            }
        }

        //..............................................................................//

        //converts a stored attachment into the shape the client is given
        private static AttachmentSummary Summarise(Attachment attachment)
        {
            return new AttachmentSummary
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                ContentType = attachment.ContentType,
                SizeInBytes = attachment.SizeInBytes,
                StoredSizeInBytes = attachment.StoredSizeInBytes,
                UploadedAt = attachment.UploadedAt
            };
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
