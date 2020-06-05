using DSMRParser.Converters;
using DSMRParser.Models;
using MonitorUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CosmosConverter
{
    public class TelegramV1
    {

    // /XMX5LGBBFFB231226417
    // 1-3:0.2.8(42)				//DSMR Version 4.2
    // 0-0:1.0.0(190704163934S)	//Date-time stamp of P1 Message
    // 0-0:96.1.1(4530303035303031363935303633303135)		//Equipement identifier
    // 1-0:1.8.1(006591.570*kWh)		//Meter Reading electricity delivered to client (Tariff 1) in 0,001 kWh
    // 1-0:2.8.1(000553.576*kWh)		//Meter Reading electricity delivered to client (Tariff 2) in 0,001 kWh
    // 1-0:1.8.2(004856.477*kWh)       //Meter Reading electricity delivered by client (Tariff 1) in 0,001 kWh
    // 1-0:2.8.2(001331.381*kWh)		//Meter Reading electricity delivered by client (Tariff 2) in 0,001 kWh
    // 0-0:96.14.0(0002)				//Tariff indicator electricity.
    // 1-0:1.7.0(00.000*kW)			//Actual electricity power delivered (+P) in 1 Watt resolution
    // 1-0:2.7.0(02.575*kW)			//Actual electricity power received (+P) in 1 Watt resolution
    // 0-0:96.7.21(00014)				//Number of power failures in any phase
    // 0-0:96.7.9(00007)				//Number of long power failures in any phase
    // 1-0:99.97.0(7)(0-0:96.7.19)(181004121300S)(0000004487*s)(180608105808S)(0000001183*s)(170127203820W)(0000003656*s)(160604003643S)(0000000656*s)(160510123123S)(0000000943*s)(151126095659W)(0000002444*s)(150211091555W)(0001380715*s)		//Power Failure Event Log (long power failures)
    // 1-0:32.32.0(00001)				//Number of voltage sags in phase L1
    // 1-0:52.32.0(00002)				//Number of voltage sags in phase L2
    // 1-0:72.32.0(00002)				//Number of voltage sags in phase L3
    // 1-0:32.36.0(00000)				//Number of voltage swells in phase L1
    // 1-0:52.36.0(00000)				//Number of voltage swells in phase L2
    // 1-0:72.36.0(00000)				//Number of voltage swells in phase L3
    // 0-0:96.13.1()					//Text message codes: numeric 8 digits
    // 0-0:96.13.0()					//Text message max 1024 characters
    // 1-0:31.7.0(012*A)				//Instantaneous current L1 in A resolution
    // 1-0:51.7.0(001*A)				//Instantaneous current L2 in A resolution
    // 1-0:71.7.0(000*A)				//Instantaneous current L3 in A resolution
    // 1-0:21.7.0(00.000*kW)			//Instantaneous active power L1 (+P) in W resolution
    // 1-0:41.7.0(00.143*kW)			//Instantaneous active power L2 (+P) in W resolution
    // 1-0:61.7.0(00.015*kW)			//Instantaneous active power L3 (+P) in W resolution
    // 1-0:22.7.0(02.734*kW)			//Instantaneous active power L1 (-P) in W resolution
    // 1-0:42.7.0(00.000*kW)			//Instantaneous active power L2 (-P) in W resolution
    // 1-0:62.7.0(00.000*kW)			//Instantaneous active power L3 (-P) in W resolution
    // !F498

        private const string LineEnding = "\r\n";

        [JsonPropertyName("id")]
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public string Id { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonPropertyName("messageHeader")]
        public string MessageHeader { get; set; }

        [JsonPropertyName("messageVersion")]
        [Obis("1-3:0.2.8")]
        public ObisVersion MessageVersion { get; set; }

        [JsonPropertyName("serialNumberElectricityMeter")]
        [Obis("0-0:96.1.1")]
        public string SerialNumberElectricityMeter { get; set; }

        [JsonPropertyName("serialNumberGasMeter")]
        [Obis("0-1:96.1.0")]
        public string SerialNumberGasMeter { get; set; } 

        [JsonPropertyName("timestamp")]
        [Obis("0-0:1.0.0"), TypeConverter(typeof(ObisTimestampConverter))]
        public DateTime Timestamp { get; set; }

        //Power consumption per tarriff
        [JsonPropertyName("powerConsumptionTariff1")]
        [Obis("1-0:1.8.1", ValueUnit = "kWh")]
        public double PowerConsumptionTariff1 { get; set; }
        [JsonPropertyName("powerConsumptionTariff2")]
        [Obis("1-0:1.8.2", ValueUnit = "kWh")]
        public double PowerConsumptionTariff2 { get; set; }

        //Power production per tarriff
        [JsonPropertyName("powerProductionTariff1")]
        [Obis("1-0:2.8.1", ValueUnit = "kWh")]
        public double PowerProductionTariff1 { get; set; }
        [JsonPropertyName("powerProductionTariff2")]
        [Obis("1-0:2.8.2", ValueUnit = "kWh")]
        public double PowerProductionTariff2 { get; set; }

        //Tariff code 1 is used for low tariff and tariff code 2 is used for normal tariff.
        [JsonPropertyName("currentTariff")]
        [Obis("0-0:96.14.0")]
        public PowerTariff CurrentTariff { get; set; }

        //Actual electricity power delivered (+P) in 1 Watt resolution
        [JsonPropertyName("actualPowerDelivered1")]
        [Obis("1-0:1.7.0", ValueUnit = "kW")]
        public double ActualPowerDelivered1 { get; set; }

        [JsonPropertyName("actualPowerDelivered2")]
        [Obis("1-0:2.7.0", ValueUnit = "kW")]
        public double ActualPowerDelivered2 { get; set; }

        //Instantaneous active power (+P) in W resolution per phase
        [JsonPropertyName("instantaneousElectricityUsageL1")]
        [Obis("1-0:21.7.0", ValueUnit = "kW")]
        public double InstantaneousElectricityUsageL1 { get; set; }
        [JsonPropertyName("instantaneousElectricityUsageL2")]
        [Obis("1-0:41.7.0", ValueUnit = "kW")]
        public double InstantaneousElectricityUsageL2 { get; set; }
        [JsonPropertyName("instantaneousElectricityUsageL3")]
        [Obis("1-0:61.7.0", ValueUnit = "kW")]
        public double InstantaneousElectricityUsageL3 { get; set; }

        //Instantaneous active power (-P) in W resolution per phase
        [JsonPropertyName("instantaneousElectricityDeliveryL1")]
        [Obis("1-0:22.7.0", ValueUnit = "kW")]
        public double InstantaneousElectricityDeliveryL1 { get; set; }
        [JsonPropertyName("instantaneousElectricityDeliveryL2")]
        [Obis("1-0:42.7.0", ValueUnit = "kW")]
        public double InstantaneousElectricityDeliveryL2 { get; set; }
        [JsonPropertyName("instantaneousElectricityDeliveryL3")]
        [Obis("1-0:62.7.0", ValueUnit = "kW")]
        public double InstantaneousElectricityDeliveryL3 { get; set; }


        //Number of voltage sags in phase
        [JsonPropertyName("voltageSagsL1")]
        [Obis("1-0:32.32.0")]
        public int VoltageSagsL1 { get; set; }
        [JsonPropertyName("voltageSagsL2")]
        [Obis("1-0:52.32.0")]
        public int VoltageSagsL2 { get; set; }
        [JsonPropertyName("voltageSagsL3")]
        [Obis("1-0:72.32.0")]
        public int VoltageSagsL3 { get; set; }

        //Number of voltage swells in phase
        [JsonPropertyName("voltageSwellsL1")]
        [Obis("1-0:32.36.0")]
        public int VoltageSwellsL1 { get; set; }
        [JsonPropertyName("voltageSwellsL2")]
        [Obis("1-0:52.36.0")]
        public int VoltageSwellsL2 { get; set; }
        [JsonPropertyName("voltageSwellsL3")]
        [Obis("1-0:72.36.0")]
        public int VoltageSwellsL3 { get; set; }


        //Message code and text
        [JsonPropertyName("messageCode")]
        [Obis("0-0:96.13.1")]
        public string MessageCode { get; set; }
        [JsonPropertyName("messageText")]
        [Obis("0-0:96.13.0")]
        public string MessageText { get; set; }

        //Instantaneous current in A resolution per phase
        [JsonPropertyName("instantaneousCurrentL1")]
        [Obis("1-0:31.7.0", ValueUnit = "A")]
        public double InstantaneousCurrentL1 { get; set; }
        [JsonPropertyName("instantaneousCurrentL2")]
        [Obis("1-0:51.7.0", ValueUnit = "A")]
        public double InstantaneousCurrentL2 { get; set; }
        [JsonPropertyName("instantaneousCurrentL3")]
        [Obis("1-0:71.7.0", ValueUnit = "A")]
        public double InstantaneousCurrentL3 { get; set; }

        //Number of power failures in any phase
        [JsonPropertyName("powerFailures")]
        [Obis("0-0:96.7.21")]
        public int PowerFailures { get; set; }

        //Number of long power failures in any phase
        [JsonPropertyName("longPowerFailures")]
        [Obis("0-0:96.7.9")]
        public int LongPowerFailures { get; set; }

        //Power Failure Event Log (long power failures)
        [JsonPropertyName("numberOfLogEntries")]
        [Obis("1-0:99.97.0")]
        public int NumberOfLogEntries { get; set; }

        [JsonPropertyName("powerFailureEvents")]
        [Obis("0-0:96.7.19")]
        public IList<PowerFailureEvent> PowerFailureEvents { get; set; }

        [JsonPropertyName("gasUsage")]
        [Obis("0-1:24.2.1", 1, "m3")]
        public double GasUsage { get; set; } 
        [JsonPropertyName("gasTimestamp")]
        [Obis("0-1:24.2.1", 0), TypeConverter(typeof(ObisTimestampConverter))]
        public DateTime GasTimestamp { get; set; }
        [JsonPropertyName("crc")]
        public string CRC { get; set; } = string.Empty;

        [JsonIgnore]
        public IList<string> Lines { get; set; } = new List<string>();

        public override string ToString()
        {
            return string.Join(LineEnding, Lines) + LineEnding;
        }

        public string ComputeChecksum()
        {
            var crc = new Crc16(Crc16Mode.IBM_REVERSED);

            byte[] bytes = Encoding.ASCII.GetBytes(ToString()[0..^6]);
            var checksum = crc.ComputeChecksumBytes(bytes);
            return BitConverter.ToString(checksum, 1, 1) + BitConverter.ToString(checksum, 0, 1);
        }
    }

    [TypeConverter(typeof(ObisTariffConverter))]
    public enum PowerTariff
    {
        Low = 1,
        Normal = 2
    }

    [TypeConverter(typeof(ObisVersionConverter))]
    public enum ObisVersion
    {
        V20,
        V42,
        V50
    }
}

