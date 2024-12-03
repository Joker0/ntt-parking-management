using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace ParkingManagementSystem
{
    // Enum for vehicle type
    public enum VehicleType
    {
        Mobil,
        Motor
    }


    // Vehicle class representing a parked vehicle
    public class Vehicle
    {
        public string RegistrationNumber { get; }
        public string Color { get; }
        public VehicleType Type { get; }
        public DateTime EntryTime { get; }

        public int ParkFee { get; }

        public Vehicle(string registrationNumber, string color, VehicleType type)
        {
            RegistrationNumber = registrationNumber;
            Color = color;
            Type = type;
            EntryTime = DateTime.Now;
            if (type == VehicleType.Mobil) ParkFee = 10000;
            else ParkFee = 5000;

        }

        public bool HasOddPlateNumber() =>
            int.Parse(RegistrationNumber.Split('-')[1][0].ToString()) % 2 != 0;

        public bool HasEvenPlateNumber() =>
            int.Parse(RegistrationNumber.Split('-')[1][0].ToString()) % 2 == 0;
    }

    // Interface for parking lot operations
    public interface IParkingLot
    {
        void CreateLot(int slots);
        int ParkVehicle(Vehicle vehicle);
        void RemoveVehicle(int slotNumber);
        IEnumerable<Vehicle> GetParkedVehicles();
    }

    // Concrete implementation of parking lot
    public class ParkingLot : IParkingLot
    {
        private readonly List<Vehicle?> _slots;
        public int Capacity { get; private set; }

        public ParkingLot(int capacity)
        {
            Capacity = capacity;
            _slots = new List<Vehicle?>(new Vehicle?[capacity]);
        }

        public void CreateLot(int slots)
        {
            Capacity = slots;
            _slots.Clear();
            _slots.AddRange(new Vehicle?[slots]);
            Console.WriteLine($"Created a parking lot with {slots} slots");
        }

        public int ParkVehicle(Vehicle vehicle)
        {
            if (_slots.Count(s => s != null) >= Capacity)
            {
                Console.WriteLine("Sorry, parking lot is full");
                return -1;
            }

            int slotNumber = _slots.IndexOf(null) + 1;
            _slots[slotNumber - 1] = vehicle;
            Console.WriteLine($"Allocated slot number: {slotNumber}");
            return slotNumber;
        }

        public void RemoveVehicle(int slotNumber)
        {
            if (slotNumber > 0 && slotNumber <= _slots.Count)
            {
                _slots[slotNumber - 1] = null;
                Console.WriteLine($"Slot number {slotNumber} is free");
            }
        }

        public IEnumerable<Vehicle> GetParkedVehicles() =>
            _slots.Where(v => v != null).Select(v => v!);
    }

    public class ParkingRates
    {
        public decimal CarHourlyRate { get; set; }
        public decimal MotorcycleHourlyRate { get; set; }

        // Default constructor with sample rates
        public ParkingRates()
        {
            CarHourlyRate = 10000m;        // Rate for cars
            MotorcycleHourlyRate = 5000m; // Rate for motorcycles
        }
    }
    // Parking lot query service
    public class ParkingService
    {
        private readonly ParkingLot _parkingLot;

        private readonly ParkingRates _rates;

        public ParkingService(ParkingLot parkingLot, ParkingRates? rates = null)
        {
            _parkingLot = parkingLot;
            _rates = rates ?? new ParkingRates();
        }

        public void DisplayStatus()
        {
            if (_parkingLot.GetParkedVehicles().Count() == 0)
            {
                Console.WriteLine("Parking lot is empty");
                return;
            }
            Console.Clear();
            Console.WriteLine("Slot No. Type Registration No Colour Fee");
            foreach (var (vehicle, index) in _parkingLot.GetParkedVehicles().Select((v, i) => (v, i)))
            {
                Console.WriteLine($"{index + 1} {vehicle.RegistrationNumber} {vehicle.Type} {vehicle.Color} {vehicle.ParkFee}");
            }
        }

        public int CountVehiclesByType(VehicleType type) =>
            _parkingLot.GetParkedVehicles().Count(v => v.Type == type);

        public IEnumerable<string> GetRegistrationNumbersByPlateType(bool isOdd) =>
            _parkingLot.GetParkedVehicles()
                .Where(v => isOdd ? v.HasOddPlateNumber() : v.HasEvenPlateNumber())
                .Select(v => v.RegistrationNumber);

        public IEnumerable<string> GetRegistrationNumbersByColor(string color) =>
            _parkingLot.GetParkedVehicles()
                .Where(v => v.Color.Equals(color, StringComparison.OrdinalIgnoreCase))
                .Select(v => v.RegistrationNumber);

        public IEnumerable<int> GetSlotNumbersByColor(string color) =>
            _parkingLot.GetParkedVehicles()
                .Select((v, index) => (Vehicle: v, Index: index))
                .Where(x => x.Vehicle.Color.Equals(color, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Index + 1);

        public int GetSlotNumberByRegistration(string registrationNumber) =>
            _parkingLot.GetParkedVehicles()
                .Select((v, index) => (Vehicle: v, Index: index))
                .FirstOrDefault(x => x.Vehicle.RegistrationNumber == registrationNumber)
                .Index + 1;

        public void DisplayCommandList()
        {
            Console.Clear();
            Console.WriteLine("Commands:");
            Console.WriteLine("1. create_parking_lot <number>");
            Console.WriteLine("2. park <registration_number> <color> <type>");
            Console.WriteLine("3. leave <slot_number>");
            Console.WriteLine("4. status");
            Console.WriteLine("5. registration_numbers_for_cars_with_odd_plate");
            Console.WriteLine("6. registration_numbers_for_cars_with_even_plate");
            Console.WriteLine("7. slot_numbers_for_cars_with_colour <color>");
            Console.WriteLine("8. slot_number_for_registration_number <registration_number>");
            Console.WriteLine("9. exit");
        }

        public decimal CalculateParkingFee(Vehicle vehicle)
        {
            TimeSpan duration = DateTime.Now - vehicle.EntryTime;
            int hours = Math.Max(1, (int)Math.Ceiling(duration.TotalHours));

            // Different rates based on vehicle type
            decimal hourlyRate = vehicle.Type switch
            {
                VehicleType.Mobil => _rates.CarHourlyRate,
                VehicleType.Motor => _rates.MotorcycleHourlyRate,
                _ => throw new ArgumentException("Unknown vehicle type")
            };

            return hours * hourlyRate;
        }

        public string GenerateParkingReceipt(Vehicle vehicle)
        {
            decimal fee = CalculateParkingFee(vehicle);
            return $"Vehicle: {vehicle.RegistrationNumber}\n" +
               $"Type: {vehicle.Type}\n" +
               $"Entry Time: {vehicle.EntryTime}\n" +
               $"Duration: {Math.Max(1, (int)Math.Ceiling((DateTime.Now - vehicle.EntryTime).TotalHours))} hours\n" +
               $"Hourly Rate: {GetHourlyRateForVehicle(vehicle):C0}\n" +
               $"Total Fee: {fee:C0}";
        }
        private decimal GetHourlyRateForVehicle(Vehicle vehicle)
        {
            return vehicle.Type switch
            {
                VehicleType.Mobil => _rates.CarHourlyRate,
                VehicleType.Motor => _rates.MotorcycleHourlyRate,
                _ => throw new ArgumentException("Unknown vehicle type")
            };
        }
    }

    // Command handler and application entry point
    public class ParkingApplication
    {
        private readonly ParkingLot _parkingLot;
        private readonly ParkingService _parkingService;

        public ParkingApplication()
        {
            _parkingLot = new ParkingLot(0);
            _parkingService = new ParkingService(_parkingLot);
        }

        public void Run()
        {

            while (true)
            {
                Console.Write("Input command: ");
                try
                {
                    string[] input = Console.ReadLine()!.Split(' ');
                    ProcessCommand(input);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private void ProcessCommand(string[] input)
        {
            string command = input[0];

            switch (command)
            {
                case "create_parking_lot":
                    _parkingLot.CreateLot(int.Parse(input[1]));
                    break;
                case "park":
                    ParkVehicle(input);
                    break;
                case "leave":
                    _parkingLot.RemoveVehicle(int.Parse(input[1]));
                    break;
                case "status":
                    _parkingService.DisplayStatus();
                    break;
                case "type_of_vehicles":
                    HandleTypeOfVehicles(input);
                    break;
                case "registration_numbers_for_vehicles_with_odd_plate":
                    DisplayRegistrationNumbers(true);
                    break;
                case "registration_numbers_for_vehicles_with_even_plate":
                    DisplayRegistrationNumbers(false);
                    break;
                case "registration_numbers_for_vehicles_with_colour":
                    DisplayRegistrationNumbersByColor(input[1]);
                    break;
                case "slot_numbers_for_vehicles_with_colour":
                    DisplaySlotNumbersByColor(input[1]);
                    break;
                case "slot_number_for_registration_number":
                    DisplaySlotNumberByRegistration(input[1]);
                    break;
                case "parking_receipt":
                    DisplayParkingReceipts(input);
                    break;
                case "command_list":
                    _parkingService.DisplayCommandList();
                    break;
                case "exit":
                    Environment.Exit(0);
                    break;
                default:
                    Console.WriteLine("Invalid command");
                    break;
            }
        }

        private void ParkVehicle(string[] input)
        {
            var vehicle = new Vehicle(
                input[1],
                input[2],
                Enum.TryParse(input[3], out VehicleType type) ? type : VehicleType.Mobil
            );
            _parkingLot.ParkVehicle(vehicle);
        }

        private void HandleTypeOfVehicles(string[] input)
        {
            if (Enum.TryParse(input[1], out VehicleType vehicleType))
            {
                Console.WriteLine(_parkingService.CountVehiclesByType(vehicleType));
            }
        }

        private void DisplayRegistrationNumbers(bool isOdd)
        {
            Console.WriteLine(string.Join(", ", _parkingService.GetRegistrationNumbersByPlateType(isOdd)));
        }

        private void DisplayRegistrationNumbersByColor(string color)
        {
            Console.WriteLine(string.Join(", ", _parkingService.GetRegistrationNumbersByColor(color)));
        }

        private void DisplaySlotNumbersByColor(string color)
        {
            Console.WriteLine(string.Join(", ", _parkingService.GetSlotNumbersByColor(color)));
        }

        private void DisplaySlotNumberByRegistration(string registrationNumber)
        {
            int slotNumber = _parkingService.GetSlotNumberByRegistration(registrationNumber);
            Console.WriteLine(slotNumber > 0 ? slotNumber.ToString() : "Not found");
        }

        private void DisplayParkingReceipts(string[] input)
        {
            
            Vehicle vehicle = _parkingLot.GetParkedVehicles().FirstOrDefault(v => v.RegistrationNumber == input[1]);
            if (vehicle == null)
            {
                Console.WriteLine($"Vehicle with registration number {input[1]} not found");
                return;
            }
           
            string receipt =_parkingService.GenerateParkingReceipt(vehicle);
            Console.WriteLine(receipt);
        }

        static void Main()
        {
            Console.Clear();
            Console.WriteLine("Welcome to Parking Management System");
            Console.WriteLine("Type 'command_list' for a list of available commands.");
            new ParkingApplication().Run();
        }
    }
}