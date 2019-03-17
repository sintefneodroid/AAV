using droid.Runtime.Prototyping.Actors;
using droid.Runtime.Prototyping.Evaluation;
using droid.Runtime.Utilities.GameObjects.Sensors;
using UnityEngine;

namespace AAV.Drone
{
    public class DroneEvaluation : ObjectiveFunction
    {
        [SerializeField] Actor _actor;

        [SerializeField] Rigidbody _actor_rigidbody;

        [SerializeField] Collider _actor_collider;

        [SerializeField] bool _hit =false;

        public override float InternalEvaluate(){
            if (this._hit){
                this.ParentEnvironment.Terminate("Drone hit an obstacle");
                return -10;
            }

            return this._actor_rigidbody.velocity.magnitude;
        }

        public override void InternalReset(){
            this._hit = false;
        }

        protected override void PostSetup()
        {
            base.PostSetup();

            if (this._actor == null){
                this._actor = FindObjectOfType<Actor>();
            }

            if (this._actor){

                if (this._actor_rigidbody == null)
                {
                    this._actor_rigidbody = this._actor.GetComponent<Rigidbody>();
                }

                if (this._actor_collider == null)
                {
                    this._actor_collider = this._actor.GetComponent<Collider>();
                }

                if (this._actor_collider != null){
                    var publisher = this._actor_collider.gameObject.GetComponent<ChildCollider3DSensor>();
                    if(!publisher|| publisher.Caller != this)
                    {
                        publisher = this._actor_collider.gameObject.AddComponent<ChildCollider3DSensor>();
                    }

                    publisher.Caller = this;
                    publisher.OnCollisionEnterDelegate = this.OnChildCollision;
                }
            }
        }


        void OnChildCollision(GameObject childSensorGameObject, Collision collision){
            this._hit = true;

#if NEODROID_DEBUG
              if (this.Debugging) {
                Debug.Log(this._hit);
              }
#endif
        }
    }
}
