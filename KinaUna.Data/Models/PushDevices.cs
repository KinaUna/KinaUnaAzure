﻿namespace KinaUna.Data.Models
{
    // Source: https://github.com/coryjthompson/WebPushDemo/tree/master/WebPushDemo
    /// <summary>
    /// Entity Framework Entity for PushDevice data.
    /// </summary>
    public class PushDevices
    {
        public int Id { get; init; }
        public string Name { get; set; }
        public string PushEndpoint { get; init; }
        public string PushP256DH { get; init; }
        public string PushAuth { get; init; }
    }
}
