using System.Collections.Concurrent;
using SmartX.Api.Models.Attachments;

//ST10445500 - PROG7312 - SmartX POE
//AttachmentStore

//.....................................o0oSTART OF FILEo0o........................................//

// The store keeps gateway data in memory instead of using a database.

namespace SmartX.Api.Services
{
    //keeps track of which files belong to which sensor, and where the encrypted copies live
    //descriptions in memory, the files themselves on disk
    public class AttachmentStore
    {
        private readonly ConcurrentDictionary<string, List<Attachment>> _bySensor =
            new ConcurrentDictionary<string, List<Attachment>>(StringComparer.OrdinalIgnoreCase);

        private readonly string _folder;

        //..............................................................................//

        public AttachmentStore(IWebHostEnvironment environment)
        {
            _folder = Path.Combine(environment.ContentRootPath, "uploads");
            Directory.CreateDirectory(_folder);
        }

        //..............................................................................//

        //retrieves where the encrypted copy of a file sits on disk
        public string PathFor(string storedFileName)
        {
            return Path.Combine(_folder, storedFileName);
        }

        //..............................................................................//

        //records a file against the sensor it was attached to
        public void Add(Attachment attachment)
        {
            var forSensor = _bySensor.GetOrAdd(attachment.MacAddress, _ => new List<Attachment>());

            // ConcurrentDictionary protects the dictionary, not the list inside it.
            lock (forSensor)
            {
                forSensor.Add(attachment);
            }
        }

        //..............................................................................//

        //retrieves every file attached to a sensor, newest first
        public List<Attachment> GetFor(string macAddress)
        {
            if (!_bySensor.TryGetValue(macAddress, out var forSensor))
            {
                return new List<Attachment>();
            }

            lock (forSensor)
            {
                return forSensor.OrderByDescending(attachment => attachment.UploadedAt).ToList();
            }
        }

        //..............................................................................//

        //forgets a file, handing back what was removed so the caller can delete it
        public Attachment? Remove(string macAddress, string id)
        {
            if (!_bySensor.TryGetValue(macAddress, out var forSensor))
            {
                return null;
            }

            lock (forSensor)
            {
                var found = forSensor.FirstOrDefault(attachment => attachment.Id == id);

                if (found != null)
                {
                    forSensor.Remove(found);
                }

                return found;
            }
        }

        //..............................................................................//

        //retrieves one file attached to a sensor, or null if that sensor has no such file
        public Attachment? Find(string macAddress, string id)
        {
            if (!_bySensor.TryGetValue(macAddress, out var forSensor))
            {
                return null;
            }

            lock (forSensor)
            {
                return forSensor.FirstOrDefault(attachment => attachment.Id == id);
            }
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
