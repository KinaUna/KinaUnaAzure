using System.ComponentModel.DataAnnotations;

namespace KinaUna.Data.Models
{
    // Source: https://github.com/coryjthompson/WebPushDemo/tree/master/WebPushDemo
    /// <summary>
    /// Entity Framework Entity for PushDevice data.
    /// </summary>
    public class PushDevices
    {
        public int Id { get; init; }

        [MaxLength(256)]
        public string Name { get; set; }

        [MaxLength(4096)]
        public string PushEndpoint { get; init; }

        [MaxLength(4096)]
        public string PushP256DH { get; init; }

        [MaxLength(4096)]
        public string PushAuth { get; init; }
    }
}
