
using System;

namespace ExternalNonChangableLibrary
{
	abstract public class DataObjectRoot
	{
		public DataObjectInfo Details { get; set; } = new DataObjectInfo { };
	}
}
