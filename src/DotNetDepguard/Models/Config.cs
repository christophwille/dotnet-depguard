using System;
using Newtonsoft.Json;

namespace DotNetDepguard.Models
{
	public partial class Config
	{
		[JsonProperty("packages")]
		public string[] Packages { get; set; }
	}
}