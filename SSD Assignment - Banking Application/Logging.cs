using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Security.Principal;

namespace SSD_Assignment___Banking_Application
{
    internal class Logging
    {

        private const string SourceName = "SSD Assignment Banking Application";
        private const string LogName = "Application";

        public static void SetupEventSource()
        {
            if (!EventLog.SourceExists(SourceName))
            {
                EventLog.CreateEventSource(SourceName, LogName);
                Console.WriteLine($"Event source '{SourceName}' created in log '{LogName}'.");
            }
        }

        private static string GetWindowsSID()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            return identity.User?.Value ?? "SID_NOT_FOUND";
        }
        public static void LogTransaction(string bankTellerName, string accountNumber, string accountHolderName, string transactionType, DateTime transactionDateTime, string reason, string appMetadata, double amount)
        {
            string logMessage = $@"
                WHO:
                    Bank Teller: {bankTellerName}
                    Account No: {accountNumber}
                    Account Holder: {accountHolderName}

                WHAT: Transaction Type: {transactionType}

                WHERE: Device Identifier: {GetWindowsSID()}

                WHEN: Date/Time: {transactionDateTime:yyyy-MM-dd HH:mm:ss}

                HOW: Application Metadata: {appMetadata}";
            if (amount > 10000 && !string.IsNullOrEmpty(reason))
            {
                logMessage += $@"WHY: (Reason for > €10,000 Transaction): {reason}";
            }

            try
            {
                EventLog.WriteEntry(SourceName, logMessage, EventLogEntryType.Information);
                Console.WriteLine("Transaction logged successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log transaction: {ex.Message}");
            }
        }

    }
}
