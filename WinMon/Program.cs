using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Microsoft.Win32;

namespace WinMon
{

	public class Program
	{
		
		static readonly string[] suffixes = { "KB", "MB", "GB", "TB", "PB" };
		public static string FormatSize(Int64 bytes)
		{
			int counter = 0;
			decimal number = (decimal)bytes;
			while (Math.Round(number / 1024) >= 1)
			{
				number = number / 1024;
				counter++;
			}
			return string.Format("{0:n1}{1}", number, suffixes[counter]);
		}
		
		static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
		static string SizeSuffix(Int64 value)
		{
			if (value < 0) { return "-" + SizeSuffix(-value); }
			if (value == 0) { return "0.0 bytes"; }

			int mag = (int)Math.Log(value, 1024);
			decimal adjustedSize = (decimal)value / (1L << (mag * 10));

			return string.Format("{0:n1} {1}", adjustedSize, SizeSuffixes[mag]);
		}
		
		
		public static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");
			XmlWriter writer = XmlWriter.Create("SystemInfo.xml");
			writer.WriteStartElement("root"); // root start
			

			writer.WriteStartElement("OperatingSystem"); // OperatingSystem start
			ManagementObjectSearcher mos = new ManagementObjectSearcher("select * from Win32_OperatingSystem");
			foreach (ManagementObject managementObject in mos.Get())
			{
				//            	foreach (PropertyData prop in managementObject.Properties){
//			        Console.WriteLine("{0}: {1}", prop.Name, prop.Value);
//			    }
				if (managementObject["Caption"] != null)
				{
					writer.WriteElementString("OperatingSystemName", managementObject["Caption"].ToString());
				}
				if (managementObject["OSArchitecture"] != null)
				{
					writer.WriteElementString("OperatingSystemArchitecture", managementObject["OSArchitecture"].ToString());
				}
				if (managementObject["CSName"] != null)
				{
					writer.WriteElementString("ComputerName", managementObject["CSName"].ToString());
				}
				if (managementObject["Manufacturer"] != null)
				{
					writer.WriteElementString("Manufacturer", managementObject["Manufacturer"].ToString());
				}
				if (managementObject["SerialNumber"] != null)
				{
					writer.WriteElementString("SerialNumber", managementObject["SerialNumber"].ToString());
				}
				if (managementObject["InstallDate"] != null)
				{
					writer.WriteElementString("InstallDate", managementObject["InstallDate"].ToString());
				}
				if (managementObject["LastBootUpTime"] != null)
				{
					writer.WriteElementString("LastBootUpTime", managementObject["LastBootUpTime"].ToString());
				}
			}
			writer.WriteElementString("UserName", Environment.UserName);
			writer.WriteEndElement(); // OperatingSystem end

			writer.WriteStartElement("PowerStatus"); // PowerStatus start
			Type t = typeof(System.Windows.Forms.PowerStatus);
			PropertyInfo[] pi = t.GetProperties();
			for( int i=0; i<pi.Length; i++ )
			{
				object propval = pi[i].GetValue(SystemInformation.PowerStatus, null);
				if (propval == null){
					return;
				}
				if(pi[i].Name.Equals("PowerLineStatus")){
					writer.WriteElementString(pi[i].Name, propval.ToString());
				}
				if(pi[i].Name.Equals("BatteryChargeStatus")){
					writer.WriteElementString(pi[i].Name, propval.ToString());
				}
				if(pi[i].Name.Equals("BatteryLifePercent")){
					writer.WriteElementString(pi[i].Name, propval.ToString());
				}
				if(pi[i].Name.Equals("BatteryLifeRemaining")){
					writer.WriteElementString(pi[i].Name, propval.ToString());
				}
			}
			writer.WriteEndElement(); // PowerStatus end
			
			
			writer.WriteStartElement("Hardware"); // Hardware start
			
			writer.WriteStartElement("Ram"); // Ram start
			
			writer.WriteStartElement("RamUsage"); // RamUsage start
			ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
			foreach (ManagementObject result in searcher.Get())
			{
				writer.WriteElementString("FreeSpaceInPagingFiles", FormatSize(Convert.ToInt64(result["FreeSpaceInPagingFiles"])));
				writer.WriteElementString("FreeVirtualMemory", FormatSize(Convert.ToInt64(result["FreeVirtualMemory"])));
				writer.WriteElementString("SizeStoredInPagingFiles", FormatSize(Convert.ToInt64(result["SizeStoredInPagingFiles"])));
				writer.WriteElementString("TotalSwapSpaceSize", FormatSize(Convert.ToInt64(result["TotalSwapSpaceSize"])));
				writer.WriteElementString("TotalVirtualMemorySize", FormatSize(Convert.ToInt64(result["TotalVirtualMemorySize"])));
				writer.WriteElementString("TotalVisibleMemorySize", FormatSize(Convert.ToInt64(result["TotalVisibleMemorySize"])));
			}
			writer.WriteEndElement(); // RamUsage end
			
			writer.WriteStartElement("RamInformation"); // RamInformation start
			ManagementObjectSearcher searcher12 =new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PhysicalMemory");
			int ct=0;
			foreach (ManagementObject queryObj in searcher12.Get())
			{
				ct++;
				writer.WriteStartElement("RamNumber"); // RamNumber start
				writer.WriteAttributeString("id", ct.ToString());
				writer.WriteElementString("Capacity", Math.Round(System.Convert.ToDouble(queryObj["Capacity"]) / 1024 / 1024 / 1024, 2).ToString());
				writer.WriteElementString("Speed", queryObj["Speed"].ToString());
				writer.WriteElementString("Manufacturer", queryObj["Manufacturer"].ToString());
				writer.WriteElementString("PartNumber", queryObj["PartNumber"].ToString());
				writer.WriteElementString("SerialNumber", queryObj["SerialNumber"].ToString());
				writer.WriteEndElement(); // RamNumber end
			}
			
			writer.WriteEndElement(); //RamInformation end
			writer.WriteEndElement(); // Ram end
			
			
			writer.WriteStartElement("NetworkAdapter"); // NetworkAdapter start
			IPGlobalProperties computerProperties = IPGlobalProperties.GetIPGlobalProperties();
			NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
			if (nics == null || nics.Length < 1)
			{
				writer.WriteElementString("Adapter", "No Adapter Found"); // Adapter not found start
				writer.WriteEndElement(); // Adapter not found end
			}else{
				int ct1=0;
				foreach (NetworkInterface adapter in nics)
				{
					ct1++;
					writer.WriteStartElement("Adapter"); // Adapter start
					writer.WriteAttributeString("id", ct1.ToString());
					IPInterfaceProperties properties = adapter.GetIPProperties();
					writer.WriteElementString("AdapterName", adapter.Description.ToString());
					writer.WriteElementString("InterfaceType", adapter.NetworkInterfaceType.ToString());
					
					writer.WriteElementString("PhysicalAddress", adapter.GetPhysicalAddress().ToString());
					
					writer.WriteElementString("NetworkInterfaceType", adapter.Name.ToString());
					
					
					string uniCast2=null;
					foreach (IPAddressInformation uniCast in properties.UnicastAddresses)
					{
						uniCast2=uniCast.Address.ToString();
					}
					writer.WriteElementString("IPAddress", uniCast2);
					writer.WriteElementString("OperationalStatus", adapter.OperationalStatus.ToString());
					writer.WriteElementString("Speed", adapter.Speed.ToString());
					writer.WriteEndElement();// Adapter end
				}
			}
			writer.WriteEndElement(); // NetworkAdapter end
			
			writer.WriteStartElement("Processor"); // Processor start
			ManagementObjectSearcher myProcessorObject = new ManagementObjectSearcher("select * from Win32_Processor");
			foreach (ManagementObject obj in myProcessorObject.Get())
			{
				writer.WriteElementString("Name", obj["Name"].ToString());
				writer.WriteElementString("DeviceID", obj["DeviceID"].ToString());
				writer.WriteElementString("Manufacturer", obj["Manufacturer"].ToString());
				writer.WriteElementString("CurrentClockSpeed", obj["CurrentClockSpeed"].ToString());
				writer.WriteElementString("Caption", obj["Caption"].ToString());
				writer.WriteElementString("NumberOfCores", obj["NumberOfCores"].ToString());
				writer.WriteElementString("NumberOfEnabledCore", obj["NumberOfEnabledCore"].ToString());
				writer.WriteElementString("NumberOfLogicalProcessors", obj["NumberOfLogicalProcessors"].ToString());
			}
			writer.WriteEndElement(); // Processor end
			
			writer.WriteStartElement("HardDisk"); // HardDisk start
			WqlObjectQuery q = new WqlObjectQuery("SELECT * FROM Win32_DiskDrive");
			ManagementObjectSearcher res = new ManagementObjectSearcher(q);
			int mt=0;
			foreach (ManagementObject o in res.Get()) {
				mt++;
				writer.WriteStartElement("HDD"); // HDD start
				writer.WriteAttributeString("id", mt.ToString());
				writer.WriteElementString("Name", o["Caption"].ToString());
				writer.WriteElementString("Manufacturer", o["Manufacturer"].ToString());
				writer.WriteElementString("MediaType", o["MediaType"].ToString());
				writer.WriteElementString("Model", o["Model"].ToString());
				writer.WriteElementString("DeviceID", o["DeviceID"].ToString());
				writer.WriteEndElement(); // HDD end
			}
			DriveInfo[] allDrives = DriveInfo.GetDrives();
			int pt=0;
			foreach (DriveInfo d in allDrives)
			{
				pt++;
				writer.WriteStartElement("Partition"); // Partition start
				writer.WriteAttributeString("id", pt.ToString());
				writer.WriteElementString("Drive", d.Name.ToString());
				writer.WriteElementString("DriveType", d.DriveType.ToString());
				if (d.IsReady == true)
				{
					writer.WriteElementString("VolumeLabel", d.VolumeLabel.ToString());
					writer.WriteElementString("AvailableSpaceUser", SizeSuffix(d.AvailableFreeSpace));
					writer.WriteElementString("TotalAvailableSpace", SizeSuffix(d.TotalFreeSpace));
					writer.WriteElementString("TotalSpace", SizeSuffix(d.TotalSize));
				}
				writer.WriteEndElement(); // Partition end
			}
			writer.WriteEndElement(); // HardDisk end
			
			writer.WriteStartElement("GPU"); // GPU start
			ManagementObjectSearcher myVideoObject = new ManagementObjectSearcher("select * from Win32_VideoController");
			foreach (ManagementObject obj in myVideoObject.Get())
			{
				writer.WriteElementString("Name",obj["Name"].ToString());
				writer.WriteElementString("Status",obj["Status"].ToString());
				writer.WriteElementString("Caption",obj["Caption"].ToString());
				writer.WriteElementString("DeviceID",obj["DeviceID"].ToString());
				writer.WriteElementString("AdapterRAM",SizeSuffix((long)Convert.ToDouble(obj["AdapterRAM"])));
				writer.WriteElementString("AdapterDACType",obj["AdapterDACType"].ToString());
				writer.WriteElementString("Monochrome",obj["Monochrome"].ToString());
				writer.WriteElementString("InstalledDisplayDrivers",obj["InstalledDisplayDrivers"].ToString());
				writer.WriteElementString("DriverVersion",obj["DriverVersion"].ToString());
				writer.WriteElementString("VideoProcessor",obj["VideoProcessor"].ToString());
				writer.WriteElementString("VideoArchitecture",obj["VideoArchitecture"].ToString());
				writer.WriteElementString("VideoMemoryType",obj["VideoMemoryType"].ToString());
			}
			writer.WriteEndElement(); // GPU end
			
			
			writer.WriteStartElement("PointingDevice"); // PointingDevice start
			SelectQuery Sq = new SelectQuery("Win32_PointingDevice");
			ManagementObjectSearcher objOSDetails = new ManagementObjectSearcher(Sq);
			ManagementObjectCollection osDetailsCollection = objOSDetails.Get();
			int po=0;
			foreach (ManagementObject mo in osDetailsCollection)
			{
				po++;
				writer.WriteStartElement("PointingDeviceNo"); // PointingDeviceNo start
				writer.WriteAttributeString("id", po.ToString());
				writer.WriteElementString("Name",mo["Name"].ToString());
				writer.WriteElementString("HardwareType",mo["HardwareType"].ToString());
				writer.WriteElementString("Manufacturer",mo["Manufacturer"].ToString());
				writer.WriteElementString("PNPDeviceID",mo["PNPDeviceID"].ToString());
				writer.WriteElementString("CreationClassName",mo["CreationClassName"].ToString());
				writer.WriteEndElement(); // PointingDeviceNo end
			}
			writer.WriteEndElement(); // PointingDevice end
			
			
			writer.WriteStartElement("Keyboard"); // Keyboard start
			SelectQuery Sq2 = new SelectQuery("Win32_PointingDevice");
			ManagementObjectSearcher objOSDetails2 = new ManagementObjectSearcher(Sq2);
			ManagementObjectCollection osDetailsCollection2 = objOSDetails2.Get();
			int po2=0;
			foreach (ManagementObject mo in osDetailsCollection2)
			{
				po2++;
				writer.WriteStartElement("KeyboardeNo"); // KeyboardeNo start
				writer.WriteAttributeString("id", po2.ToString());
				writer.WriteElementString("Name",mo["Name"].ToString());
				writer.WriteElementString("Description",mo["Description"].ToString());
				writer.WriteElementString("DeviceID",mo["DeviceID"].ToString());
				writer.WriteElementString("PNPDeviceID",mo["PNPDeviceID"].ToString());
				writer.WriteEndElement(); // KeyboardeNo end
			}
			writer.WriteEndElement(); // Keyboard end
			
			writer.WriteEndElement(); // Hardware end
			
			writer.WriteStartElement("RunningProcess"); // RunningProcess start
			Process[] processlist = Process.GetProcesses();
			foreach (Process process in processlist)
			{
				if (!String.IsNullOrEmpty(process.MainWindowTitle))
				{
					writer.WriteStartElement("Process"); // Process start
					writer.WriteAttributeString("id", process.Id.ToString());
					writer.WriteElementString("Name",process.ProcessName.ToString());
					writer.WriteElementString("Title",process.MainWindowTitle.ToString());
					writer.WriteElementString("StartTime",process.StartTime.ToString());
					writer.WriteElementString("UserProcessorTime",process.UserProcessorTime.ToString());
					writer.WriteElementString("TotalProcessorTime",process.TotalProcessorTime.ToString());
					writer.WriteEndElement(); // Process end
				}
			}
			writer.WriteEndElement(); // RunningProcess end

//			writer.WriteStartElement("InstalledSoftware"); // InstalledSoftware start
//			SelectQuery Sq3 = new SelectQuery("Win32_Product");
//			ManagementObjectSearcher objOSDetails3 = new ManagementObjectSearcher(Sq3);
//			ManagementObjectCollection osDetailsCollection3 = objOSDetails3.Get();
//			foreach (ManagementObject MO in osDetailsCollection3)
//			{
//				writer.WriteElementString("Name", MO["Name"].ToString());
//				writer.WriteElementString("InstallLocation", (string)MO["InstallLocation"]);
//				writer.WriteElementString("InstallDate", MO["InstallDate"].ToString());
//				writer.WriteElementString("InstallSource", MO["InstallSource"].ToString());
//				writer.WriteElementString("PackageName", MO["PackageName"].ToString());
//				writer.WriteElementString("Vendor", MO["Vendor"].ToString());
//				writer.WriteElementString("Version", MO["Version"].ToString());
//			}
//			writer.WriteEndElement(); // InstalledSoftware end
			
			writer.WriteEndElement(); // root end
			
			writer.Flush();
			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
		}
	}
}
