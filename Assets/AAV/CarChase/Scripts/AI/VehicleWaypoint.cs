using UnityEngine;

namespace AAV.CarChase.Scripts.AI {
  [DisallowMultipleComponent]
  [AddComponentMenu("RVP/AI/Vehicle Waypoint", 1)]

  //Class for vehicle waypoints
  public class VehicleWaypoint : MonoBehaviour {
    public VehicleWaypoint nextPoint;
    public float radius = 10;

    [Tooltip("Percentage of a vehicle's max speed to drive at")]
    [Range(0, 1)]
    public float speed = 1;

    void OnDrawGizmos() {
      //Visualize waypoint
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(this.transform.position, this.radius);

      //Draw line to next point
      if (this.nextPoint) {
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(this.transform.position, this.nextPoint.transform.position);
      }
    }
  }
}
