using MelonLoader;
using HarmonyLib;
using MyBhapticsTactsuit;

using Il2CppBNG;
using Il2CppEmeraldAI;
using UnityEngine;
using Il2CppKnife.RealBlood.SimpleController;
using Il2Cpp;
using Il2CppFIMSpace.BonesStimulation;

[assembly: MelonInfo(typeof(CONVRGENCE_bhaptics.CONVRGENCE_bhaptics), "CONVRGENCE_bhaptics", "1.0.5", "Florian Fahrenberger")]
[assembly: MelonGame("Monkey-With-a-Bomb", "CONVRGENCE")]


namespace CONVRGENCE_bhaptics
{
    public class CONVRGENCE_bhaptics : MelonMod
    {
        public static TactsuitVR tactsuitVr = null!;
        public static bool isRightHanded = true;

        public override void OnInitializeMelon()
        {
            tactsuitVr = new TactsuitVR();
            tactsuitVr.PlaybackHaptics("HeartBeat");
        }

        [HarmonyPatch(typeof(RaycastWeapon), "Shoot", new Type[] { })]
        public class bhaptics_ShootWeapon
        {
            [HarmonyPostfix]
            public static void Postfix(RaycastWeapon __instance)
            {
                //tactsuitVr.LOG("Shoot: " + __instance.name + " " + __instance.BulletInChamber.ToString() + " " + __instance.readyToShoot.ToString());
                if (!__instance.IsMosin)
                {
                    if (!__instance.readyToShoot) return;
                    if (!__instance.BulletInChamber) return;
                }
                bool isRight = (__instance.thisGrabber.HandSide == ControllerHand.Right);
                bool twoHanded = ((__instance.IsSniper)|(__instance.PistolSecondHand));
                if (__instance.SecondHandGrabbable != null)
                    twoHanded = ((twoHanded)|(__instance.SecondHandGrabbable.BeingHeld));
                tactsuitVr.GunRecoil(isRight, 1f, twoHanded);
            }
        }

        
        [HarmonyPatch(typeof(PlayerBase), "DamageTake", new Type[] { typeof(int) })]
        public class bhaptics_TakeDamage
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("Impact");
            }
        }
        

        [HarmonyPatch(typeof(PlayerBase), "Healing", new Type[] { })]
        public class bhaptics_Healing
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("Healing");
            }
        }

        [HarmonyPatch(typeof(PlayerBase), "Healing2", new Type[] { })]
        public class bhaptics_Healing2
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("Healing");
            }
        }

        [HarmonyPatch(typeof(PlayerBase), "HealBleeding", new Type[] { })]
        public class bhaptics_HealBleeding
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("Healing");
            }
        }

        [HarmonyPatch(typeof(Bandage), "StartBandageProcess", new Type[] { })]
        public class bhaptics_Bandaging
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("Healing");
            }
        }

        [HarmonyPatch(typeof(PlayerBase), "Drink", new Type[] { typeof(float) })]
        public class bhaptics_Drinking
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("Drinking");
            }
        }

        [HarmonyPatch(typeof(PlayerBase), "Eat", new Type[] { typeof(float) })]
        public class bhaptics_Eating
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("Eating");
            }
        }

        [HarmonyPatch(typeof(Mouth), "PlaySmokingLoop", new Type[] {  })]
        public class bhaptics_Smoking
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("Smoking");
            }
        }

        [HarmonyPatch(typeof(PlayerBase), "AdrenalineShot", new Type[] { })]
        public class bhaptics_AdrenalineShot
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("Healing");
            }
        }

        [HarmonyPatch(typeof(PlayerBase), "StrangeSound", new Type[] { })]
        public class bhaptics_StrangeSound
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("NeckTingle");
            }
        }


        private static KeyValuePair<float, float> getAngleAndShift(Vector3 playerPosition, Vector3 hit, Quaternion playerRotation)
        {
            // bhaptics pattern starts in the front, then rotates to the left. 0° is front, 90° is left, 270° is right.
            // y is "up", z is "forward" in local coordinates
            Vector3 patternOrigin = new Vector3(0f, 0f, 1f);
            Vector3 hitPosition = hit - playerPosition;
            Quaternion myPlayerRotation = playerRotation;
            Vector3 playerDir = myPlayerRotation.eulerAngles;
            // get rid of the up/down component to analyze xz-rotation
            Vector3 flattenedHit = new Vector3(hitPosition.x, 0f, hitPosition.z);

            // get angle. .Net < 4.0 does not have a "SignedAngle" function...
            float hitAngle = Vector3.Angle(flattenedHit, patternOrigin);
            // check if cross product points up or down, to make signed angle myself
            Vector3 crossProduct = Vector3.Cross(flattenedHit, patternOrigin);
            if (crossProduct.y > 0f) { hitAngle *= -1f; }
            // relative to player direction
            float myRotation = hitAngle - playerDir.y;
            // switch directions (bhaptics angles are in mathematically negative direction)
            myRotation *= -1f;
            // convert signed angle into [0, 360] rotation
            if (myRotation < 0f) { myRotation = 360f + myRotation; }


            // up/down shift is in y-direction
            // in Shadow Legend, the torso Transform has y=0 at the neck,
            // and the torso ends at roughly -0.5 (that's in meters)
            // so cap the shift to [-0.5, 0]...
            float hitShift = hitPosition.y;
            //tactsuitVr.LOG("HitShift: " + hitShift);
            float upperBound = 0.5f;
            float lowerBound = -0.5f;
            if (hitShift > upperBound) { hitShift = 0.5f; }
            else if (hitShift < lowerBound) { hitShift = -0.5f; }
            // ...and then spread/shift it to [-0.5, 0.5]
            else { hitShift = (hitShift - lowerBound) / (upperBound - lowerBound) - 0.5f; }

            //tactsuitVr.LOG("Relative x-z-position: " + relativeHitDir.x.ToString() + " "  + relativeHitDir.z.ToString());
            //tactsuitVr.LOG("HitAngle: " + hitAngle.ToString());
            //tactsuitVr.LOG("HitShift: " + hitShift.ToString());

            // No tuple returns available in .NET < 4.0, so this is the easiest quickfix
            return new KeyValuePair<float, float>(myRotation, hitShift);
        }


        [HarmonyPatch(typeof(PlayerBase), "takeDamage")]
        public class bhaptics_Damage1
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerBase __instance)
            {
                Vector3 hitPosition = __instance.MobileBlood.transform.position;
                Vector3 playerPosition = __instance.Player.transform.position;
                Quaternion playerRotation = __instance.Player.transform.rotation;
                var angleShift = getAngleAndShift(playerPosition, hitPosition, playerRotation);
                tactsuitVr.PlayBackHit("Impact", angleShift.Key, angleShift.Value);
            }
        }


        [HarmonyPatch(typeof(PlayerBase), "takeDamage2")]
        public class bhaptics_Damage2
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerBase __instance)
            {
                Vector3 hitPosition = __instance.MobileBlood.transform.position;
                Vector3 playerPosition = __instance.Player.transform.position;
                Quaternion playerRotation = __instance.Player.transform.rotation;
                var angleShift = getAngleAndShift(playerPosition, hitPosition, playerRotation);
                tactsuitVr.PlayBackHit("Impact", angleShift.Key, angleShift.Value);
            }
        }

        [HarmonyPatch(typeof(BackpackAutoSorting), "SnapLoot", new Type[] { })]
        public class bhaptics_SnapLoot
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerBase __instance)
            {
                string pattern = "StoreShoulder";
                if (isRightHanded) pattern += "_L";
                else pattern += "_R";
                tactsuitVr.PlaybackHaptics(pattern);
            }
        }

        [HarmonyPatch(typeof(SnapZone), "GrabEquipped", new Type[] { typeof(Grabber) })]
        public class bhaptics_GrabEquipped
        {
            [HarmonyPostfix]
            public static void Postfix(SnapZone __instance, Grabber grabber)
            {
                if (__instance == null) return;
                string pattern = "";
                string leftSuffix = "_L";
                string rightSuffix = "_R";
                if (!isRightHanded) { leftSuffix = "_R"; rightSuffix = "_L"; }
                switch (__instance.name)
                {
                    case "KnifeHolster":
                        pattern = "HolsterHip" + leftSuffix;
                        break;
                    case "FlashlightHolster":
                        pattern = "HolsterChest" + leftSuffix;
                        break;
                    case "BookHolster":
                        pattern = "HolsterChest" + rightSuffix;
                        break;
                    case "ArtContainerHolster":
                        pattern = "HolsterBack";
                        break;
                    case "RevolverHolster":
                        pattern = "HolsterHip" + rightSuffix;
                        break;
                    case "PistolHolster":
                        pattern = "HolsterHip" + rightSuffix;
                        break;
                    case "KEDRHolster":
                        pattern = "HolsterHip" + rightSuffix;
                        break;
                    case "StechkinHolster":
                        pattern = "HolsterHip" + rightSuffix;
                        break;
                    case "TokarevHolster":
                        pattern = "HolsterHip" + rightSuffix;
                        break;
                    case "BackpackHolster":
                        pattern = "ReceiveShoulder" + leftSuffix;
                        break;
                    case "HeavyWeaponHolster":
                        pattern = "ReceiveShoulder" + rightSuffix;
                        break;
                    case "SaigaHolster":
                        pattern = "ReceiveShoulder" + rightSuffix;
                        break;
                    case "SVDHolster":
                        pattern = "ReceiveShoulder" + rightSuffix;
                        break;
                    case "PPSHolster":
                        pattern = "ReceiveShoulder" + rightSuffix;
                        break;
                    case "CrossbowHolster":
                        pattern = "ReceiveShoulder" + rightSuffix;
                        break;
                    case "ObrezHolster":
                        pattern = "ReceiveShoulder" + rightSuffix;
                        break;
                    case "MosinHolster":
                        pattern = "ReceiveShoulder" + rightSuffix;
                        break;
                    case "FlamethrowerHolster":
                        pattern = "ReceiveShoulder" + rightSuffix;
                        break;
                    case "EnergyRifleHolster":
                        pattern = "ReceiveShoulder" + rightSuffix;
                        break;
                    default:
                        pattern = "";
                        break;
                }
                if (pattern == "") return;
                tactsuitVr.PlaybackHaptics(pattern);
            }
        }

        [HarmonyPatch(typeof(BonesStimulator), "Vibrate_ExplosionShake", new Type[] { typeof(float) })]
        public class bhaptics_Explosion
        {
            [HarmonyPostfix]
            public static void Postfix(BonesStimulator __instance)
            {
                tactsuitVr.PlaybackHaptics("Explosion");
            }
        }


        [HarmonyPatch(typeof(SnapZone), "GrabGrabbable", new Type[] { typeof(Grabbable) })]
        public class bhaptics_GrabGrabbable
        {
            [HarmonyPostfix]
            public static void Postfix(SnapZone __instance)
            {
                if (__instance == null) return;
                string pattern = "";
                string leftSuffix = "_L";
                string rightSuffix = "_R";
                if (!isRightHanded) { leftSuffix = "_R"; rightSuffix = "_L"; }
                switch (__instance.name)
                {
                    case "KnifeHolster":
                        pattern = "HolsterHip" + leftSuffix;
                        break;
                    case "FlashlightHolster":
                        pattern = "HolsterChest" + leftSuffix;
                        break;
                    case "BookHolster":
                        pattern = "HolsterChest" + rightSuffix;
                        break;
                    case "ArtContainerHolster":
                        pattern = "HolsterBack";
                        break;
                    case "RevolverHolster":
                        pattern = "HolsterHip" + rightSuffix;
                        break;
                    case "PistolHolster":
                        pattern = "HolsterHip" + rightSuffix;
                        break;
                    case "KEDRHolster":
                        pattern = "HolsterHip" + rightSuffix;
                        break;
                    case "StechkinHolster":
                        pattern = "HolsterHip" + rightSuffix;
                        break;
                    case "TokarevHolster":
                        pattern = "HolsterHip" + rightSuffix;
                        break;
                    case "BackpackHolster":
                        pattern = "ReceiveShoulder" + leftSuffix;
                        break;
                    case "HeavyWeaponHolster":
                        pattern = "ReceiveShoulder" + rightSuffix;
                        break;
                    case "SaigaHolster":
                        pattern = "ReceiveShoulder" + rightSuffix;
                        break;
                    case "SVDHolster":
                        pattern = "ReceiveShoulder" + rightSuffix;
                        break;
                    case "PPSHolster":
                        pattern = "ReceiveShoulder" + rightSuffix;
                        break;
                    case "CrossbowHolster":
                        pattern = "ReceiveShoulder" + rightSuffix;
                        break;
                    case "ObrezHolster":
                        pattern = "ReceiveShoulder" + rightSuffix;
                        break;
                    case "MosinHolster":
                        pattern = "ReceiveShoulder" + rightSuffix;
                        break;
                    case "FlamethrowerHolster":
                        pattern = "ReceiveShoulder" + rightSuffix;
                        break;
                    case "EnergyRifleHolster":
                        pattern = "ReceiveShoulder" + rightSuffix;
                        break;
                    default:
                        pattern = "";
                        break;
                }
                if (pattern == "") return;
                tactsuitVr.PlaybackHaptics(pattern);
            }
        }

        [HarmonyPatch(typeof(Mouth), "StartWhistle")]
        public class bhaptics_Whistle
        {
            [HarmonyPostfix]
            public static void Postfix(Mouth __instance)
            {
                tactsuitVr.PlaybackHaptics("WhistleShort");
            }
        }
        [HarmonyPatch(typeof(Mouth), "StartLongWhistle")]
        public class bhaptics_Whistle2
        {
            [HarmonyPostfix]
            public static void Postfix(Mouth __instance)
            {
                tactsuitVr.PlaybackHaptics("WhistleLong");
            }
        }


        [HarmonyPatch(typeof(PlayerBase), "UpdateHPBar", new Type[] { })]
        public class bhaptics_UpdateHealth
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerBase __instance)
            {
                if (__instance.HP <= 25.0f) tactsuitVr.StartHeartBeat();
                else tactsuitVr.StopHeartBeat();
            }
        }

        [HarmonyPatch(typeof(InGameMenu), "LeftHanded", new Type[] { })]
        public class bhaptics_LeftHandedInGame
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                isRightHanded = false;
            }
        }

        [HarmonyPatch(typeof(InGameMenu), "RightHanded", new Type[] { })]
        public class bhaptics_RightHandedInGame
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                isRightHanded = true;
            }
        }

        [HarmonyPatch(typeof(MainMenu), "LeftHanded", new Type[] { })]
        public class bhaptics_LeftHanded
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                isRightHanded = false;
            }
        }

        [HarmonyPatch(typeof(MainMenu), "RightHanded", new Type[] { })]
        public class bhaptics_RightHanded
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                isRightHanded = true;
            }
        }

    }
}