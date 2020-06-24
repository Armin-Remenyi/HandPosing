﻿using UnityEngine;

namespace PoseAuthoring
{
    [System.Serializable]
    public struct CylinderSurface
    {
        [SerializeField]
        private Vector3 _startPoint;
        [SerializeField]
        private Vector3 _endPoint;
        [SerializeField]
        private float _angle;

        public Transform Grip { get; set; }


        public CylinderSurface(Transform grip)
        {
            this.Grip = grip;

            _angle = 230f;
            _startPoint = Vector3.up * 0.2f;
            _endPoint = Vector3.down * 0.2f;
        }

        public Vector3 StartAngleDir
        {
            get
            {
                return Vector3.Cross(Direction, Vector3.forward).normalized;
            }
        }
        public Vector3 EndAngleDir
        {
            get
            {
                return Quaternion.AngleAxis(Angle, Direction) * StartAngleDir;
            }
        }

        public Vector3 StartPoint
        {
            get
            {
                return this.Grip.TransformPoint(_startPoint);
            }
            set
            {
                _startPoint = this.Grip.InverseTransformPoint(value);
            }
        }

        public Vector3 EndPoint
        {
            get
            {
                return this.Grip.TransformPoint(_endPoint);
            }
            set
            {
                _endPoint = this.Grip.InverseTransformPoint(value);
            }
        }

        public float Angle
        {
            get
            {
                return _angle;
            }
            set
            {
                _angle = Mathf.Repeat(value, 360f);
            }
        }

        public float Radious
        {
            get
            {
                Vector3 start = StartPoint;
                Vector3 projectedPoint = start + Vector3.Project(this.Grip.position - start, Direction);
                return Vector3.Distance(projectedPoint, this.Grip.position);
            }
        }

        public float Height
        {
            get
            {
                return (EndPoint - StartPoint).magnitude;
            }
        }

        public Vector3 Direction
        {
            get
            {
                Vector3 dir = (EndPoint - StartPoint);
                if (dir.sqrMagnitude == 0f)
                {
                    return this.Grip.up;
                }
                return dir.normalized;
            }
        }

        public void MakeSinglePoint()
        {
            _startPoint = _endPoint = Vector3.zero;
            Angle = 0f;
        }

        public Vector3 NearestPointInSurface(Vector3 targetPosition)
        {
            Vector3 start = StartPoint;
            Vector3 dir = Direction;
            Vector3 projectedVector = Vector3.Project(targetPosition - start, dir);

            if (projectedVector.magnitude > Height)
            {
                projectedVector = projectedVector.normalized * Height;
            }
            if (Vector3.Dot(projectedVector, dir) < 0f)
            {
                projectedVector = Vector3.zero;
            }

            Vector3 projectedPoint = StartPoint + projectedVector;
            Vector3 targetDirection = Vector3.ProjectOnPlane((targetPosition - projectedPoint), dir).normalized;
            //clamp of the surface
            float desiredAngle = Mathf.Repeat(Vector3.SignedAngle(StartAngleDir, targetDirection, dir), 360f);
            if (desiredAngle > Angle)
            {
                if (Mathf.Abs(desiredAngle - Angle) >= Mathf.Abs(360f - desiredAngle))
                {
                    targetDirection = StartAngleDir;
                }
                else
                {
                    targetDirection = EndAngleDir;
                }
            }

            Vector3 surfacePoint = projectedPoint + targetDirection * Radious;
            return surfacePoint;
        }

        public Quaternion TransformRotation(HandSnapPose pose, Transform relativeTo, Vector3 atSurfacePoint)
        {
            Vector3 globalPos = relativeTo.TransformPoint(pose.relativeGripPos);
            Quaternion globalRot = relativeTo.rotation * pose.relativeGripRot;

            Vector3 entrySurfacePoint = NearestPointInSurface(globalPos);
            Vector3 entryDirection = Vector3.ProjectOnPlane(entrySurfacePoint - StartPoint, Direction);
            Vector3 desiredDirection = Vector3.ProjectOnPlane(atSurfacePoint - StartPoint, Direction);
            Quaternion offsetDirection = Quaternion.FromToRotation(entryDirection, desiredDirection);

            return offsetDirection * globalRot;
        }
    }
}