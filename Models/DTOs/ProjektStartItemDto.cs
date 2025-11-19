using System;
using System.Collections.Generic;

namespace ZUMI_Backend.Models.DTOs
{
    
    public class ProjektStartItemDto
    {
        public Guid ProjektId { get; set; }
        public string Kurztitel { get; set; } = default!;
        public string? Titelbild { get; set; }
        public List<Guid> SdgIds { get; set; } = new();

        // 0 = Owner, 1 = Liked, 2 = Participating
        public int Category { get; set; }
    }
}
