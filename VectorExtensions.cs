﻿using System;
using GTA;
using GTA.Math;
using GTA.Native;

namespace MapEditor
{
    public static class VectorExtensions
    {
        public static Vector3 ForwardVector(this Vector3 vector, float yaw)
        {
            Vector3 right;
            float cos = (float)Math.Cos(yaw + Math.PI/2.0f);
            right.X = (180f/(float)Math.PI)*cos;
            right.Y = 0f;
            float sin = (float) Math.Sin(yaw + Math.PI/2.0f);
            right.Z = (180f/(float) Math.PI)*sin;
            return CrossWith(vector, right);
        }

        public static Vector3 CrossWith(Vector3 left, Vector3 right)
        {
            Vector3 result;
            result.X = left.Y*right.Z - left.Z*right.Y;
            result.Y = left.Z*right.X - left.X*right.Z;
            result.Z = left.X*right.Y - left.Y*right.X;
            return result;
        }

        public static bool WorldToScreenRel(Vector3 worldCoords, out Vector2 screenCoords)
        {
            OutputArgument num1 = new OutputArgument();
            OutputArgument num2 = new OutputArgument();
            if (!Function.Call<bool>(Hash._WORLD3D_TO_SCREEN2D, worldCoords.X, worldCoords.Y, worldCoords.Z, num1, num2))
            {
                screenCoords = new Vector2();
                return false;
            }
            screenCoords = new Vector2((num1.GetResult<float>() - 0.5f) * 2, (num2.GetResult<float>() - 0.5f) * 2);
            return true;
        }

        public static Vector3 ScreenRelToWorld(Vector3 camPos, Vector3 camRot, Vector2 coord)
        {
            var camForward = RotationToDirection(camRot);
            var rotUp = camRot + new Vector3(10, 0, 0);
            var rotDown = camRot + new Vector3(-10, 0, 0);
            var rotLeft = camRot + new Vector3(0, 0, -10);
            var rotRight = camRot + new Vector3(0, 0, 10);

            var camRight = RotationToDirection(rotRight) - RotationToDirection(rotLeft);
            var camUp = RotationToDirection(rotUp) - RotationToDirection(rotDown);

            var rollRad = -DegToRad(camRot.Y);

            var camRightRoll = camRight * (float)Math.Cos(rollRad) - camUp * (float)Math.Sin(rollRad);
            var camUpRoll = camRight * (float)Math.Sin(rollRad) + camUp * (float)Math.Cos(rollRad);

            var point3D = camPos + camForward * 10.0f + camRightRoll + camUpRoll;
            Vector2 point2D;
            if (!WorldToScreenRel(point3D, out point2D)) return camPos + camForward * 10.0f;
            var point3DZero = camPos + camForward * 10.0f;
            Vector2 point2DZero;
            if (!WorldToScreenRel(point3DZero, out point2DZero)) return camPos + camForward * 10.0f;

            const double eps = 0.001;
            if (Math.Abs(point2D.X - point2DZero.X) < eps || Math.Abs(point2D.Y - point2DZero.Y) < eps) return camPos + camForward * 10.0f;
            var scaleX = (coord.X - point2DZero.X) / (point2D.X - point2DZero.X);
            var scaleY = (coord.Y - point2DZero.Y) / (point2D.Y - point2DZero.Y);
            var point3Dret = camPos + camForward * 10.0f + camRightRoll * scaleX + camUpRoll * scaleY;
            return point3Dret;
        }

        public static Vector3 RotationToDirection(Vector3 rotation)
        {
            var z = DegToRad(rotation.Z);
            var x = DegToRad(rotation.X);
            var num = Math.Abs(Math.Cos(x));
            return new Vector3
            {
                X = (float)(-Math.Sin(z) * num),
                Y = (float)(Math.Cos(z) * num),
                Z = (float)Math.Sin(x)
            };
        }

        public static Vector3 DirectionToRotation(Vector3 direction)
        {
            direction.Normalize();

            var x = Math.Atan2(direction.Z, direction.Y);
            var y = 0;
            var z = -Math.Atan2(direction.X, direction.Y);

            return new Vector3
            {
                X = (float)RadToDeg(x),
                Y = (float)RadToDeg(y),
                Z = (float)RadToDeg(z)
            };
        }

        public static double DegToRad(double deg)
        {
            return deg * Math.PI / 180.0;
        }

        public static double RadToDeg(double deg)
        {
            return deg * 180.0 / Math.PI;
        }

        public static double BoundRotationDeg(double angleDeg)
        {
            var twoPi = (int)(angleDeg / 360);
            var res = angleDeg - twoPi * 360;
            if (res < 0) res += 360;
            return res;
        }

        public static Vector3 RaycastEverything(Vector2 screenCoord)
        {
            var camPos = GameplayCamera.Position;
            var camRot = GameplayCamera.Rotation;
            const float raycastToDist = 100.0f;
            const float raycastFromDist = 1f;

            var target3D = ScreenRelToWorld(camPos, camRot, screenCoord);
            var source3D = camPos;

            Entity ignoreEntity = Game.Player.Character;
            if (Game.Player.Character.IsInVehicle())
            {
                ignoreEntity = Game.Player.Character.CurrentVehicle;
            }

            var dir = (target3D - source3D);
            dir.Normalize();
            var raycastResults = World.Raycast(source3D + dir * raycastFromDist,
                source3D + dir * raycastToDist,
                (IntersectOptions)(1 | 16 | 256 | 2 | 4 | 8)// | peds + vehicles
                , ignoreEntity);

            if (raycastResults.DitHitAnything)
            {
                return raycastResults.HitCoords;
            }

            return camPos + dir * raycastToDist;
        }

        public static Vector3 RaycastEverything(Vector2 screenCoord, Vector3 camPos, Vector3 camRot, Entity toIgnore)
        {
            const float raycastToDist = 100.0f;
            const float raycastFromDist = 1f;

            var target3D = ScreenRelToWorld(camPos, camRot, screenCoord);
            var source3D = camPos;

            Entity ignoreEntity = toIgnore;

            var dir = (target3D - source3D);
            dir.Normalize();
            var raycastResults = World.Raycast(source3D + dir * raycastFromDist,
                source3D + dir * raycastToDist,
                (IntersectOptions)(1 | 16 | 256 | 2 | 4 | 8)// | peds + vehicles
                , ignoreEntity);

            if (raycastResults.DitHitAnything)
            {
                return raycastResults.HitCoords;
            }

            return camPos + dir * raycastToDist;
        }

        public static Entity RaycastEntity(Vector2 screenCoord, Vector3 camPos, Vector3 camRot)
        {
            const float raycastToDist = 100.0f;
            const float raycastFromDist = 1f;

            var target3D = ScreenRelToWorld(camPos, camRot, screenCoord);
            var source3D = camPos;

            Entity ignoreEntity = Game.Player.Character;

            var dir = (target3D - source3D);
            dir.Normalize();
            var raycastResults = World.Raycast(source3D + dir * raycastFromDist,
                source3D + dir * raycastToDist,
                (IntersectOptions)(1 | 16 | 256 | 2 | 4 | 8)// | peds + vehicles
                , ignoreEntity);

            if (raycastResults.DitHitEntity)
            {
                return raycastResults.HitEntity;
            }

            return null;
        }
    }
}