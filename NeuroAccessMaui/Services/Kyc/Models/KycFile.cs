using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuroAccessMaui.Services.Kyc.Models
{
	public class KycFile
	{
		public required string Filename { get; set; }

		public required byte[] Data { get; set; }

		public string ContentType { get; set; } = "application/octet-stream";
	}
}
