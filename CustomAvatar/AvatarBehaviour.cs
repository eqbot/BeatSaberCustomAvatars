using AvatarScriptPack;
using CustomAvatar.Tracking;
using DynamicOpenVR.IO;
using System;
using UnityEngine;

namespace CustomAvatar
{
	public class AvatarBehaviour : MonoBehaviour
	{
		public static Pose? LeftLegCorrection { get; set; }
		public static Pose? RightLegCorrection { get; set; }
		public static Pose? PelvisCorrection { get; set; }

		private Transform _head;
		private Transform _body;
		private Transform _leftHand;
		private Transform _rightHand;
		private Transform _leftLeg;
		private Transform _rightLeg;
		private Transform _pelvis;

		private Vector3 _prevBodyPos;

		private Vector3 _prevLeftLegPos = default(Vector3);
		private Vector3 _prevRightLegPos = default(Vector3);
		private Quaternion _prevLeftLegRot = default(Quaternion);
		private Quaternion _prevRightLegRot = default(Quaternion);

		private Vector3 _prevPelvisPos = default(Vector3);
		private Quaternion _prevPelvisRot = default(Quaternion);

		private VRIK _vrik;
		private IKManagerAdvanced _ikManagerAdvanced;
		private TrackedDeviceManager _trackedDevices;
		private VRPlatformHelper _vrPlatformHelper;
		private Animator _animator;
		private PoseManager _poseManager;

		public void Start()
		{
			_vrik = GetComponentInChildren<VRIK>();
			_ikManagerAdvanced = GetComponentInChildren<IKManagerAdvanced>();
			_animator = GetComponentInChildren<Animator>() ?? throw new NullReferenceException("Avatar is missing an Animator");
			_poseManager = GetComponentInChildren<PoseManager>();

			_trackedDevices = PersistentSingleton<TrackedDeviceManager>.instance;
			_vrPlatformHelper = PersistentSingleton<VRPlatformHelper>.instance;

			_trackedDevices.DeviceAdded += (device) => UpdateVrikReferences();
			_trackedDevices.DeviceRemoved += (device) => UpdateVrikReferences();

			_head = GetHeadTransform();
			_body = gameObject.transform.Find("Body");
			_leftHand = gameObject.transform.Find("LeftHand");
			_rightHand = gameObject.transform.Find("RightHand");
			_leftLeg = gameObject.transform.Find("LeftLeg");
			_rightLeg = gameObject.transform.Find("RightLeg");
			_pelvis = gameObject.transform.Find("Pelvis");

			UpdateVrikReferences();
		}

		private void UpdateVrikReferences()
		{
			if (!_ikManagerAdvanced) return;

			Plugin.Logger.Info("Tracking device change detected, updating VRIK references");

			if (_trackedDevices.LeftFoot.Found)
			{
				_vrik.solver.leftLeg.target = _ikManagerAdvanced.LeftLeg_target;
				_vrik.solver.leftLeg.positionWeight = _ikManagerAdvanced.LeftLeg_positionWeight;
				_vrik.solver.leftLeg.rotationWeight = _ikManagerAdvanced.LeftLeg_rotationWeight;
			}
			else
			{
				_vrik.solver.leftLeg.target = null;
				_vrik.solver.leftLeg.positionWeight = 0;
				_vrik.solver.leftLeg.rotationWeight = 0;
			}

			if (_trackedDevices.RightFoot.Found)
			{
				_vrik.solver.rightLeg.target = _ikManagerAdvanced.RightLeg_target;
				_vrik.solver.rightLeg.positionWeight = _ikManagerAdvanced.RightLeg_positionWeight;
				_vrik.solver.rightLeg.rotationWeight = _ikManagerAdvanced.RightLeg_rotationWeight;
			}
			else
			{
				_vrik.solver.rightLeg.target = null;
				_vrik.solver.rightLeg.positionWeight = 0;
				_vrik.solver.rightLeg.rotationWeight = 0;
			}

			if (_trackedDevices.Waist.Found)
			{
				_vrik.solver.spine.pelvisTarget = _ikManagerAdvanced.Spine_pelvisTarget;
				_vrik.solver.spine.pelvisPositionWeight = _ikManagerAdvanced.Spine_pelvisPositionWeight;
				_vrik.solver.spine.pelvisRotationWeight = _ikManagerAdvanced.Spine_pelvisRotationWeight;
				_vrik.solver.plantFeet = false;
			}
			else
			{
				_vrik.solver.spine.pelvisTarget = null;
				_vrik.solver.spine.pelvisPositionWeight = 0;
				_vrik.solver.spine.pelvisRotationWeight = 0;
				_vrik.solver.plantFeet = true;
			}
		}

		public void ApplyFingerTracking()
		{
			if (_poseManager == null) return;

			SkeletalSummaryData leftHandAnim;
			SkeletalSummaryData rightHandAnim;

			try
			{
				leftHandAnim = Plugin.LeftHandAnimAction.GetSummaryData();
				rightHandAnim = Plugin.RightHandAnimAction.GetSummaryData();
			}
			catch (Exception)
			{
				return;
			}

			ApplyBodyBonePose(HumanBodyBones.LeftThumbProximal,       _poseManager.OpenHand_Left_ThumbProximal,       _poseManager.ClosedHand_Left_ThumbProximal,       leftHandAnim.ThumbCurl * 2);
			ApplyBodyBonePose(HumanBodyBones.LeftThumbIntermediate,   _poseManager.OpenHand_Left_ThumbIntermediate,   _poseManager.ClosedHand_Left_ThumbIntermediate,   leftHandAnim.ThumbCurl * 2);
			ApplyBodyBonePose(HumanBodyBones.LeftThumbDistal,         _poseManager.OpenHand_Left_ThumbDistal,         _poseManager.ClosedHand_Left_ThumbDistal,         leftHandAnim.ThumbCurl * 2);

			ApplyBodyBonePose(HumanBodyBones.LeftIndexProximal,       _poseManager.OpenHand_Left_IndexProximal,       _poseManager.ClosedHand_Left_IndexProximal,       leftHandAnim.IndexCurl);
			ApplyBodyBonePose(HumanBodyBones.LeftIndexIntermediate,   _poseManager.OpenHand_Left_IndexIntermediate,   _poseManager.ClosedHand_Left_IndexIntermediate,   leftHandAnim.IndexCurl);
			ApplyBodyBonePose(HumanBodyBones.LeftIndexDistal,         _poseManager.OpenHand_Left_IndexDistal,         _poseManager.ClosedHand_Left_IndexDistal,         leftHandAnim.IndexCurl);

			ApplyBodyBonePose(HumanBodyBones.LeftMiddleProximal,      _poseManager.OpenHand_Left_MiddleProximal,      _poseManager.ClosedHand_Left_MiddleProximal,      leftHandAnim.MiddleCurl);
			ApplyBodyBonePose(HumanBodyBones.LeftMiddleIntermediate,  _poseManager.OpenHand_Left_MiddleIntermediate,  _poseManager.ClosedHand_Left_MiddleIntermediate,  leftHandAnim.MiddleCurl);
			ApplyBodyBonePose(HumanBodyBones.LeftMiddleDistal,        _poseManager.OpenHand_Left_MiddleDistal,        _poseManager.ClosedHand_Left_MiddleDistal,        leftHandAnim.MiddleCurl);

			ApplyBodyBonePose(HumanBodyBones.LeftRingProximal,        _poseManager.OpenHand_Left_RingProximal,        _poseManager.ClosedHand_Left_RingProximal,        leftHandAnim.RingCurl);
			ApplyBodyBonePose(HumanBodyBones.LeftRingIntermediate,    _poseManager.OpenHand_Left_RingIntermediate,    _poseManager.ClosedHand_Left_RingIntermediate,    leftHandAnim.RingCurl);
			ApplyBodyBonePose(HumanBodyBones.LeftRingDistal,          _poseManager.OpenHand_Left_RingDistal,          _poseManager.ClosedHand_Left_RingDistal,          leftHandAnim.RingCurl);

			ApplyBodyBonePose(HumanBodyBones.LeftLittleProximal,      _poseManager.OpenHand_Left_LittleProximal,      _poseManager.ClosedHand_Left_LittleProximal,      leftHandAnim.LittleCurl);
			ApplyBodyBonePose(HumanBodyBones.LeftLittleIntermediate,  _poseManager.OpenHand_Left_LittleIntermediate,  _poseManager.ClosedHand_Left_LittleIntermediate,  leftHandAnim.LittleCurl);
			ApplyBodyBonePose(HumanBodyBones.LeftLittleDistal,        _poseManager.OpenHand_Left_LittleDistal,        _poseManager.ClosedHand_Left_LittleDistal,        leftHandAnim.LittleCurl);

			ApplyBodyBonePose(HumanBodyBones.RightThumbProximal,      _poseManager.OpenHand_Right_ThumbProximal,      _poseManager.ClosedHand_Right_ThumbProximal,      rightHandAnim.ThumbCurl * 2);
			ApplyBodyBonePose(HumanBodyBones.RightThumbIntermediate,  _poseManager.OpenHand_Right_ThumbIntermediate,  _poseManager.ClosedHand_Right_ThumbIntermediate,  rightHandAnim.ThumbCurl * 2);
			ApplyBodyBonePose(HumanBodyBones.RightThumbDistal,        _poseManager.OpenHand_Right_ThumbDistal,        _poseManager.ClosedHand_Right_ThumbDistal,        rightHandAnim.ThumbCurl * 2);

			ApplyBodyBonePose(HumanBodyBones.RightIndexProximal,      _poseManager.OpenHand_Right_IndexProximal,      _poseManager.ClosedHand_Right_IndexProximal,      rightHandAnim.IndexCurl);
			ApplyBodyBonePose(HumanBodyBones.RightIndexIntermediate,  _poseManager.OpenHand_Right_IndexIntermediate,  _poseManager.ClosedHand_Right_IndexIntermediate,  rightHandAnim.IndexCurl);
			ApplyBodyBonePose(HumanBodyBones.RightIndexDistal,        _poseManager.OpenHand_Right_IndexDistal,        _poseManager.ClosedHand_Right_IndexDistal,        rightHandAnim.IndexCurl);

			ApplyBodyBonePose(HumanBodyBones.RightMiddleProximal,     _poseManager.OpenHand_Right_MiddleProximal,     _poseManager.ClosedHand_Right_MiddleProximal,     rightHandAnim.MiddleCurl);
			ApplyBodyBonePose(HumanBodyBones.RightMiddleIntermediate, _poseManager.OpenHand_Right_MiddleIntermediate, _poseManager.ClosedHand_Right_MiddleIntermediate, rightHandAnim.MiddleCurl);
			ApplyBodyBonePose(HumanBodyBones.RightMiddleDistal,       _poseManager.OpenHand_Right_MiddleDistal,       _poseManager.ClosedHand_Right_MiddleDistal,       rightHandAnim.MiddleCurl);

			ApplyBodyBonePose(HumanBodyBones.RightRingProximal,       _poseManager.OpenHand_Right_RingProximal,       _poseManager.ClosedHand_Right_RingProximal,       rightHandAnim.RingCurl);
			ApplyBodyBonePose(HumanBodyBones.RightRingIntermediate,   _poseManager.OpenHand_Right_RingIntermediate,   _poseManager.ClosedHand_Right_RingIntermediate,   rightHandAnim.RingCurl);
			ApplyBodyBonePose(HumanBodyBones.RightRingDistal,         _poseManager.OpenHand_Right_RingDistal,         _poseManager.ClosedHand_Right_RingDistal,         rightHandAnim.RingCurl);

			ApplyBodyBonePose(HumanBodyBones.RightLittleProximal,     _poseManager.OpenHand_Right_LittleProximal,     _poseManager.ClosedHand_Right_LittleProximal,     rightHandAnim.LittleCurl);
			ApplyBodyBonePose(HumanBodyBones.RightLittleIntermediate, _poseManager.OpenHand_Right_LittleIntermediate, _poseManager.ClosedHand_Right_LittleIntermediate, rightHandAnim.LittleCurl);
			ApplyBodyBonePose(HumanBodyBones.RightLittleDistal,       _poseManager.OpenHand_Right_LittleDistal,       _poseManager.ClosedHand_Right_LittleDistal,       rightHandAnim.LittleCurl);
		}

		public void ApplyBodyBonePose(HumanBodyBones bodyBone, Pose open, Pose closed, float position)
		{
			if (_animator == null) return;

			Transform transform = _animator.GetBoneTransform(bodyBone);

			if (!transform) return;

			transform.localPosition = Vector3.Lerp(open.position, closed.position, position);
			transform.localRotation = Quaternion.Slerp(open.rotation, closed.rotation, position);
		}

		private void LateUpdate()
		{
			ApplyFingerTracking();

			try
			{
				TrackedDeviceState headPosRot = _trackedDevices.Head;
				TrackedDeviceState leftPosRot = _trackedDevices.LeftHand;
				TrackedDeviceState rightPosRot = _trackedDevices.RightHand;

				if (_head && headPosRot != null && headPosRot.NodeState.tracked)
				{
					_head.position = headPosRot.Position;
					_head.rotation = headPosRot.Rotation;
				}

				if (_leftHand && leftPosRot != null && leftPosRot.NodeState.tracked)
				{
					_leftHand.position = leftPosRot.Position;
					_leftHand.rotation = leftPosRot.Rotation;

					_vrPlatformHelper.AdjustPlatformSpecificControllerTransform(_leftHand);
				}

				if (_rightHand && rightPosRot != null && rightPosRot.NodeState.tracked)
				{
					_rightHand.position = rightPosRot.Position;
					_rightHand.rotation = rightPosRot.Rotation;

					_vrPlatformHelper.AdjustPlatformSpecificControllerTransform(_rightHand);
				}

				TrackedDeviceState leftFoot = _trackedDevices.LeftFoot;
				TrackedDeviceState rightFoot = _trackedDevices.RightFoot;
				TrackedDeviceState pelvis = _trackedDevices.Waist;

				if (_leftLeg && leftFoot != null && leftFoot.NodeState.tracked)
				{
					var leftLegPosRot = _trackedDevices.LeftFoot;
					var correction = LeftLegCorrection ?? default;

					_prevLeftLegPos = Vector3.Lerp(_prevLeftLegPos, leftLegPosRot.Position + correction.position, 15 * Time.deltaTime);
					_prevLeftLegRot = Quaternion.Slerp(_prevLeftLegRot, leftLegPosRot.Rotation * correction.rotation, 10 * Time.deltaTime);
					_leftLeg.position = _prevLeftLegPos;
					_leftLeg.rotation = _prevLeftLegRot;
				}

				if (_rightLeg && rightFoot != null && rightFoot.NodeState.tracked)
				{
					var rightLegPosRot = _trackedDevices.RightFoot;
					var correction = RightLegCorrection ?? default;

					_prevRightLegPos = Vector3.Lerp(_prevRightLegPos, rightLegPosRot.Position + correction.position, 15 * Time.deltaTime);
					_prevRightLegRot = Quaternion.Slerp(_prevRightLegRot, rightLegPosRot.Rotation * correction.rotation, 10 * Time.deltaTime);
					_rightLeg.position = _prevRightLegPos;
					_rightLeg.rotation = _prevRightLegRot;
				}

				if (_pelvis && pelvis != null && pelvis.NodeState.tracked)
				{
					var pelvisPosRot = _trackedDevices.Waist;
					var correction = PelvisCorrection ?? default;

					_prevPelvisPos = Vector3.Lerp(_prevPelvisPos, pelvisPosRot.Position + correction.position, 17 * Time.deltaTime);
					_prevPelvisRot = Quaternion.Slerp(_prevPelvisRot, pelvisPosRot.Rotation * correction.rotation, 13 * Time.deltaTime);
					_pelvis.position = _prevPelvisPos;
					_pelvis.rotation = _prevPelvisRot;
				}

				if (_body == null) return;
				_body.position = _head.position - (_head.transform.up * 0.1f);

				var vel = new Vector3(_body.transform.localPosition.x - _prevBodyPos.x, 0.0f,
					_body.localPosition.z - _prevBodyPos.z);

				var rot = Quaternion.Euler(0.0f, _head.localEulerAngles.y, 0.0f);
				var tiltAxis = Vector3.Cross(gameObject.transform.up, vel);
				_body.localRotation = Quaternion.Lerp(_body.localRotation,
					Quaternion.AngleAxis(vel.magnitude * 1250.0f, tiltAxis) * rot,
					Time.deltaTime * 10.0f);

				_prevBodyPos = _body.transform.localPosition;
			}
			catch (Exception e)
			{
				Plugin.Logger.Error($"{e.Message}\n{e.StackTrace}");
			}
		}

		private Transform GetHeadTransform()
		{
			var descriptor = GetComponent<AvatarDescriptor>();
			if (descriptor != null)
			{
				//if (descriptor.ViewPoint != null) return descriptor.ViewPoint;
			}

			return gameObject.transform.Find("Head");
		}
	}
}
