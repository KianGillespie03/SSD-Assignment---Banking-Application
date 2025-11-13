using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using SSD_Assignment___Banking_Application;


namespace Banking_Application
{
    public class Data_Access_Layer
    {

        private List<Bank_Account> accounts;
        public static String databaseName = "Banking Database.db";
        private static Data_Access_Layer instance = new Data_Access_Layer();

        private static readonly byte[] encryptionKey;
        private static readonly byte[] encryptionIV;

        static Data_Access_Layer()
        {
            string keyFile = "encryption.key";
            encryptionKey = new byte[32];
            encryptionIV = new byte[16];

            if (File.Exists(keyFile))
            {
                encryptionKey = File.ReadAllBytes(keyFile);
                Console.WriteLine("Encryption Key Loaded: " + Convert.ToBase64String(encryptionKey));
                return;
            }
            else
            {
                RandomNumberGenerator.Fill(encryptionKey);
                File.WriteAllBytes(keyFile, encryptionKey);
                Console.WriteLine("Encryption Key Generated: " + Convert.ToBase64String(encryptionKey));
            }

            RandomNumberGenerator.Fill(encryptionIV);
        }
        private Data_Access_Layer()//Singleton Design Pattern (For Concurrency Control) - Use getInstance() Method Instead.
        {
            accounts = new List<Bank_Account>();
        }

        public static Data_Access_Layer getInstance()
        {
            return instance;
        }

        private SqliteConnection getDatabaseConnection()
        {

            String databaseConnectionString = new SqliteConnectionStringBuilder()
            {
                DataSource = Data_Access_Layer.databaseName,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();

            return new SqliteConnection(databaseConnectionString);

        }

        private void initialiseDatabase()
        {
            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    CREATE TABLE IF NOT EXISTS Bank_Accounts(    
                        accountNo TEXT PRIMARY KEY,
                        name TEXT NOT NULL,
                        address_line_1 TEXT,
                        address_line_2 TEXT,
                        address_line_3 TEXT,
                        town TEXT NOT NULL,
                        balance REAL NOT NULL,
                        accountType INTEGER NOT NULL,
                        overdraftAmount REAL,
                        interestRate REAL
                    ) WITHOUT ROWID
                ";

                command.ExecuteNonQuery();
                
            }
        }

        public void loadBankAccounts()
        {
            if (!File.Exists(Data_Access_Layer.databaseName))
                initialiseDatabase();
            else
            {

                using (var connection = getDatabaseConnection())
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT * FROM Bank_Accounts";
                    SqliteDataReader dr = command.ExecuteReader();
                    
                    while(dr.Read())
                    {

                        int accountType = dr.GetInt16(7);
                        string encryptedAccNo = dr.GetString(0);
                        string encryptedName = dr.GetString(1);
                        string encryptedAddr1 = dr.GetString(2);
                        string encryptedAddr2 = dr.GetString(3);
                        string encryptedAddr3 = dr.GetString(4);
                        string encryptedTown = dr.GetString(5);

                        
                        string decryptedAccNo = EncryptionService.DecryptString(encryptedAccNo, encryptionKey);
                        string decryptedName = EncryptionService.DecryptString(encryptedName, encryptionKey);
                        string decryptedAddr1 = EncryptionService.DecryptString(encryptedAddr1, encryptionKey);
                        string decryptedAddr2 = EncryptionService.DecryptString(encryptedAddr2, encryptionKey);
                        string decryptedAddr3 = EncryptionService.DecryptString(encryptedAddr3, encryptionKey);
                        string decryptedTown = EncryptionService.DecryptString(encryptedTown, encryptionKey);


                        if (accountType == Account_Type.Current_Account)
                        {
                            Current_Account ca = new Current_Account();
                            ca.accountNo = decryptedAccNo;
                            ca.name = decryptedName;
                            ca.address_line_1 = decryptedAddr1;
                            ca.address_line_2 = decryptedAddr2;
                            ca.address_line_3 = decryptedAddr3;
                            ca.town = decryptedTown;
                            ca.balance = dr.GetDouble(6);
                            ca.overdraftAmount = dr.GetDouble(8);
                            accounts.Add(ca);
                        }
                        else
                        {
                            Savings_Account sa = new Savings_Account();
                            sa.accountNo = decryptedAccNo;
                            sa.name = decryptedName;
                            sa.address_line_1 = decryptedAddr1;
                            sa.address_line_2 = decryptedAddr2;
                            sa.address_line_3 = decryptedAddr3;
                            sa.town = decryptedTown;
                            sa.balance = dr.GetDouble(6);
                            sa.interestRate = dr.GetDouble(9);
                            accounts.Add(sa);
                        }


                    }

                }

            }
        }

        public String addBankAccount(Bank_Account ba) 
        {

            byte[] hmac;

            string encryptedAccountNo = EncryptionService.EncryptString(ba.accountNo, encryptionKey);
            string encrptedName = EncryptionService.EncryptString(ba.name, encryptionKey);
            string encryptedAddr1 = EncryptionService.EncryptString(ba.address_line_1, encryptionKey);
            string encryptedAddr2 = EncryptionService.EncryptString(ba.address_line_2, encryptionKey);
            string encryptedAddr3 = EncryptionService.EncryptString(ba.address_line_3, encryptionKey);
            string encryptedTown = EncryptionService.EncryptString(ba.town, encryptionKey);

            if (ba.GetType() == typeof(Current_Account))
                ba = (Current_Account)ba;
            else
                ba = (Savings_Account)ba;

            accounts.Add(ba);

            using (var connection = getDatabaseConnection())
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                @"
                    INSERT INTO Bank_Accounts VALUES(" +
                    "'" + encryptedAccountNo + "', " +
                    "'" + encrptedName + "', " +
                    "'" + encryptedAddr1 + "', " +
                    "'" + encryptedAddr2 + "', " +
                    "'" + encryptedAddr3 + "', " +
                    "'" + encryptedTown + "', " +
                    ba.balance + ", " +
                    (ba.GetType() == typeof(Current_Account) ? 1 : 2) + ", ";

                if (ba.GetType() == typeof(Current_Account))
                {
                    Current_Account ca = (Current_Account)ba;
                    command.CommandText += ca.overdraftAmount + ", NULL)";
                }

                else
                {
                    Savings_Account sa = (Savings_Account)ba;
                    command.CommandText += "NULL," + sa.interestRate + ")";
                }

                command.ExecuteNonQuery();

            }

            return ba.accountNo;

        }

        public Bank_Account findBankAccountByAccNo(String accNo) 
        { 
        
            foreach(Bank_Account ba in accounts)
            {

                if (ba.accountNo.Equals(accNo))
                {
                    return ba;
                }

            }

            return null; 
        }

        public bool closeBankAccount(String accNo) 
        {

            Bank_Account toRemove = null;
            
            foreach (Bank_Account ba in accounts)
            {

                if (ba.accountNo.Equals(accNo))
                {
                    toRemove = ba;
                    break;
                }

            }

            if (toRemove == null)
                return false;
            else
            {
                accounts.Remove(toRemove);

                using (var connection = getDatabaseConnection())
                {
                    connection.Open();
                    var command = connection.CreateCommand();

                    byte[] hmac;
                    string encryptedAccNo = EncryptionService.EncryptString(toRemove.accountNo, encryptionKey);

                    command.CommandText = "DELETE FROM Bank_Accounts WHERE accountNo = '" + encryptedAccNo + "'";
                    command.ExecuteNonQuery();

                }

                return true;
            }

        }

        public bool lodge(String accNo, double amountToLodge)
        {

            Bank_Account toLodgeTo = null;

            foreach (Bank_Account ba in accounts)
            {

                if (ba.accountNo.Equals(accNo))
                {
                    ba.lodge(amountToLodge);
                    toLodgeTo = ba;
                    break;
                }

            }

            if (toLodgeTo == null)
                return false;
            else
            {

                using (var connection = getDatabaseConnection())
                {
                    connection.Open();
                    var command = connection.CreateCommand();

                    byte[] hmac;
                    string encryptedAccNo = EncryptionService.EncryptString(toLodgeTo.accountNo, encryptionKey);

                    command.CommandText = "UPDATE Bank_Accounts SET balance = " + toLodgeTo.balance + " WHERE accountNo = '" + encryptedAccNo + "'";
                    command.ExecuteNonQuery();

                }

                return true;
            }

        }

        public bool withdraw(String accNo, double amountToWithdraw)
        {

            Bank_Account toWithdrawFrom = null;
            bool result = false;

            foreach (Bank_Account ba in accounts)
            {

                if (ba.accountNo.Equals(accNo))
                {
                    result = ba.withdraw(amountToWithdraw);
                    toWithdrawFrom = ba;
                    break;
                }

            }

            if (toWithdrawFrom == null || result == false)
                return false;
            else
            {

                using (var connection = getDatabaseConnection())
                {
                    connection.Open();
                    var command = connection.CreateCommand();

                    byte[] hmac;
                    string encryptedAccNo = EncryptionService.EncryptString(toWithdrawFrom.accountNo, encryptionKey);

                    command.CommandText = "UPDATE Bank_Accounts SET balance = " + toWithdrawFrom.balance + " WHERE accountNo = '" + encryptedAccNo + "'";
                    command.ExecuteNonQuery();

                }

                return true;
            }

        }

    }
}
