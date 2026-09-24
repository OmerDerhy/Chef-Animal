using UnityEngine;

namespace MergesChef.Characters
{
    public class RestaurantManager : MonoBehaviour
    {
        public static RestaurantManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            // Continuously check if we can assign deliveries
            AssignDeliveries();
        }

        private void AssignDeliveries()
        {
            // Find all kitchens in the scene
            var allKitchens = FindObjectsOfType<MergesChef.Map.KitchenAccessoryVisual>();
            
            foreach (var kitchen in allKitchens)
            {
                // Keep assigning as long as there are dishes to deliver from this kitchen
                while (kitchen.GetAvailableUnreservedDishes() > 0)
                {
                    Staff availableStaff = FindAvailableStaff();
                    Customer waitingCustomer = FindWaitingCustomer();

                    // If we are missing either staff or a customer, we can't assign this dish right now.
                    // Break the loop and wait for the next frame.
                    if (availableStaff == null || waitingCustomer == null)
                    {
                        break;
                    }

                    // We have a match!
                    kitchen.ReserveDish();
                    waitingCustomer.isFoodOnTheWay = true;
                    Debug.Log($"[RestaurantManager] Assigning {availableStaff.name} to deliver from {kitchen.name} to {waitingCustomer.name}.");
                    availableStaff.PickUpDish(kitchen, waitingCustomer);
                }
            }
        }

        private Staff FindAvailableStaff()
        {
            Staff[] allStaff = FindObjectsOfType<Staff>();
            foreach (var staff in allStaff)
            {
                if (staff.currentState == StaffState.Idle || staff.currentState == StaffState.WanderingTest)
                    return staff;
            }
            return null;
        }

        private Customer FindWaitingCustomer()
        {
            Customer[] allCustomers = FindObjectsOfType<Customer>();
            foreach (var customer in allCustomers)
            {
                if (customer.currentState == CustomerState.WaitingForFood && !customer.isFoodOnTheWay)
                    return customer;
            }
            return null;
        }
    }
}
