using UnityEngine;
using System.Collections.Generic;
using SoftFluidPuzzle.Core;
using SoftFluidPuzzle.PhysicsSimulation;

namespace SoftFluidPuzzle.PlayerControl
{
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Interaction Settings")]
        public float interactionDistance = 3f;
        public float interactionRadius = 0.5f;
        public LayerMask interactableLayers;
        public KeyCode interactKey = KeyCode.E;
        public KeyCode grabKey = KeyCode.Mouse0;

        [Header("Grabbing")]
        public float grabDistance = 3f;
        public float grabSpringStrength = 100f;
        public float grabDamping = 5f;
        public Transform holdPoint;

        [Header("UI")]
        public bool showInteractionPrompt = true;

        private PlayerInput _input;
        private IInteractable _currentInteractable;
        private GameObject _grabbedObject;
        private ConfigurableJoint _grabJoint;
        private Camera _mainCamera;

        public IInteractable CurrentInteractable => _currentInteractable;
        public bool IsGrabbing => _grabbedObject != null;

        private void Awake()
        {
            _input = GetComponent<PlayerInput>();
            _mainCamera = Camera.main;
        }

        private void Start()
        {
            if (holdPoint == null)
            {
                GameObject holdPointObj = new GameObject("HoldPoint");
                holdPointObj.transform.SetParent(transform, false);
                holdPointObj.transform.localPosition = new Vector3(0.5f, 1f, 1f);
                holdPoint = holdPointObj.transform;
            }
        }

        private void Update()
        {
            FindInteractables();

            if (_input.InteractPressed && _currentInteractable != null)
            {
                _currentInteractable.Interact(gameObject);
            }

            if (Input.GetKeyDown(grabKey))
            {
                TryGrabObject();
            }

            if (Input.GetKeyUp(grabKey))
            {
                ReleaseObject();
            }
        }

        private void FindInteractables()
        {
            Ray ray = new Ray(_mainCamera.transform.position, _mainCamera.transform.forward);
            RaycastHit hit;

            if (Physics.SphereCast(ray, interactionRadius, out hit, interactionDistance, interactableLayers))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();

                if (interactable != null && interactable.CanInteract(gameObject))
                {
                    if (_currentInteractable != interactable)
                    {
                        if (_currentInteractable != null)
                        {
                            _currentInteractable.OnFocusExit(gameObject);
                        }

                        _currentInteractable = interactable;
                        _currentInteractable.OnFocusEnter(gameObject);
                    }
                    return;
                }
            }

            if (_currentInteractable != null)
            {
                _currentInteractable.OnFocusExit(gameObject);
                _currentInteractable = null;
            }
        }

        private void TryGrabObject()
        {
            if (_grabbedObject != null) return;

            Ray ray = new Ray(_mainCamera.transform.position, _mainCamera.transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, grabDistance, interactableLayers))
            {
                Rigidbody rb = hit.rigidbody;

                if (rb != null && !rb.isKinematic)
                {
                    GrabObject(hit.collider.gameObject);
                    return;
                }

                SoftBody softBody = hit.collider.GetComponent<SoftBody>();
                if (softBody != null)
                {
                    GrabSoftBody(softBody, hit.point);
                }
            }
        }

        private void GrabObject(GameObject obj)
        {
            _grabbedObject = obj;

            _grabJoint = obj.AddComponent<ConfigurableJoint>();
            _grabJoint.connectedBody = GetComponent<Rigidbody>();

            SoftJointLimit linearLimit = new SoftJointLimit();
            linearLimit.limit = 0.1f;
            _grabJoint.linearLimit = linearLimit;

            JointDrive xDrive = new JointDrive();
            xDrive.positionSpring = grabSpringStrength;
            xDrive.positionDamper = grabDamping;
            xDrive.maximumForce = Mathf.Infinity;

            _grabJoint.xDrive = xDrive;
            _grabJoint.yDrive = xDrive;
            _grabJoint.zDrive = xDrive;

            _grabJoint.targetPosition = holdPoint.localPosition;

            IGrabable grabable = obj.GetComponent<IGrabable>();
            if (grabable != null)
            {
                grabable.OnGrabbed(gameObject);
            }
        }

        private void GrabSoftBody(SoftBody softBody, Vector3 hitPoint)
        {
            SoftBodyParticle closestParticle = FindClosestParticle(softBody, hitPoint);

            if (closestParticle != null)
            {
                closestParticle.SetStatic(true);
                _grabbedObject = softBody.gameObject;
            }
        }

        private SoftBodyParticle FindClosestParticle(SoftBody softBody, Vector3 position)
        {
            SoftBodyParticle closest = null;
            float minDistance = float.MaxValue;

            foreach (SoftBodyParticle particle in softBody.Particles)
            {
                if (particle == null) continue;

                float distance = Vector3.Distance(particle.Position, position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = particle;
                }
            }

            return closest;
        }

        private void ReleaseObject()
        {
            if (_grabbedObject == null) return;

            if (_grabJoint != null)
            {
                Destroy(_grabJoint);
                _grabJoint = null;
            }

            SoftBody softBody = _grabbedObject.GetComponent<SoftBody>();
            if (softBody != null)
            {
                foreach (SoftBodyParticle particle in softBody.Particles)
                {
                    if (particle != null && particle.IsStatic)
                    {
                        particle.SetStatic(false);
                    }
                }
            }

            IGrabable grabable = _grabbedObject.GetComponent<IGrabable>();
            if (grabable != null)
            {
                grabable.OnReleased(gameObject);
            }

            _grabbedObject = null;
        }

        public void ForceRelease()
        {
            ReleaseObject();
        }
    }

    public interface IInteractable
    {
        bool CanInteract(GameObject interactor);
        void Interact(GameObject interactor);
        void OnFocusEnter(GameObject interactor);
        void OnFocusExit(GameObject interactor);
    }

    public interface IGrabable
    {
        void OnGrabbed(GameObject grabber);
        void OnReleased(GameObject grabber);
    }
}
