using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Lean.Touch;


public class CameraTour : MonoBehaviour {

	private static CameraTour _instance;
	public static CameraTour Instance()
	{
	   	return _instance;
	}


    [Tooltip("主摄像机")]
    public Camera cameraMain;

    [Tooltip("Ignore fingers with StartedOverGui?")]
    public bool IgnoreGuiFingers = true;

    [Tooltip("Allows you to force rotation with a specific amount of fingers (0 = any)")]
    public int RequiredFingerCount;

    [Tooltip("滚轮缩放速度")]
    [Range(-1.0f, 1.0f)]
    public float WheelSensitivity = -0.08f;


	[Header("平移设置")]
	[Tooltip("x轴平移速度")]
	public float xPanSpeed = 10f;
	[Tooltip("z轴平移速度")]
	public float zPanSpeed = 10f;
    [Tooltip("x轴范围最小值")]
    public float xMin = -200f;
    [Tooltip("x轴范围最大值")]
    public float xMax = 200f;
    [Tooltip("z轴范围最小值")]
    public float zMin = -200f;
    [Tooltip("z轴范围最大值")]
    public float zMax = 200f;


	[Header("缩放设置")]
	[Tooltip("相机距离移动速度")]
	public float DistanceSpeed = 20.0f;
	[Tooltip("相机距离最小值")]
	public float DistanceMin = 2.0f;
	[Tooltip("相机距离最大值")]
	public float DistanceMax = 60.0f;


	[Header("仰角设置")]
	[Tooltip("仰角变化速率, use -1 to invert")]
	public float SensitivityHeight = 0.2f;
	[Tooltip("仰角最小值")]
	public float HeightAngleMin = 2f;
	[Tooltip("仰角最大值")]
	public float HeightAngleMax = 89f;


	[Header("鼠标右键调整时的速度系数")]
	[Tooltip("水平旋转速度")]
	public float horizontalSpeed = 2.0f;
	[Tooltip("上下仰角速度")]
	public float verticalSpeed = 2.0f;


	[Header("距离、旋转、仰角参数")]
	private float Distance;          // Desired distance (units, ie Meters)
	private float Rotation;          // Desired rotation (degrees)
	private float Tilt;              // Desired tilt (degrees)

	private float _currDistance;    // actual distance
	private float _currRotation;    // actual rotation
	private float _currTilt;        // actual tilt

	private float MinRotation = -180;      // Minimum rotation (degrees)
	private float MaxRotation = 180;       // Maximum rotation (degrees)



    //记录摄像机距离中心点的距离
	private float _cameraDest = 150.0F;
	private Vector3 _cameraCenter = Vector3.zero; //记录右键点击时的旋转中心点

    //双指旋转角度约束值
    private float twistDegreeRestrict = 0.5f;

    //标识鼠标右键是否按下
    private bool bMouseRightDown = false;

	//是否禁用全部漫游
	private bool bDisableAll = false;


	//定义委托和事件
	private bool bIsMove = false; //是否处于移动物体状态

	//委托接口
	public delegate void SetMoveStatus_(bool b);
	public event SetMoveStatus_ SetMoveStatusEvent;

	public void SetMoveStatus(bool b)
	{
		bIsMove = b;
	}

	//调用事件的方法

	/// <summary>
	/// 设置是否处于摄像机不能平移的状态,true不能平移,false恢复平移
	/// </summary>
	/// <param name="b">If set to <c>true</c> b.</param>
	public void SetDisablePan(bool b){

		if (SetMoveStatusEvent != null)
			SetMoveStatusEvent (b);	
	}

	/// <summary>
	/// 设置是否处于摄像机不能漫游的状态,true不能漫游,false恢复漫游
	/// </summary>
	/// <param name="b">If set to <c>true</c> b.</param>
	public void SetDisableAll(bool b){
		bDisableAll = b;
	}

	void Awake(){
		_instance = this;
		SetMoveStatusEvent+= SetMoveStatus;
	}

    // Use this for initialization
    void Start () {

		//初始就记录相机和地面中心的距离，用于平移时计算平移速度
		Vector3 centerPoint;
        float enter;
        if (GetInterPoint(cameraMain, new Vector3(0.5F, 0.5F, 0), out centerPoint, out enter))
        {
        	_cameraDest = enter;
        }
	}
	
	// Update is called once per frame
	void Update () {

	}

    protected virtual void LateUpdate()
    {


        // Make sure the camera exists
        if (LeanTouch.GetCamera(ref cameraMain, gameObject) == true)
        {



            // Get the fingers we want to use
            var fingers = LeanTouch.GetFingers(IgnoreGuiFingers, RequiredFingerCount);
			//return;

//            if (fingers.Count == 0)
//            {
//                return;
//            }

            //单指操作
            if (fingers.Count == 1)
            {
                if (bDisableAll)
                {
                    return;
                }


                Vector2 screenDelta = LeanGesture.GetScreenDelta(fingers);

				if (!bIsMove && !bMouseRightDown) //鼠标右键按下时，不能平移
				{
					if (Input.GetMouseButtonDown(0)) {
						_cameraDest = GetCenterDistance ();
					}

					CameraPan(screenDelta);
				}

				//鼠标右键按下时上下调整视角，左右绕中心旋转
				MouseRightUpdate ();

//				if (Input.GetMouseButton (1)) {
//					MouseRightTour ();
//				}
            }

            //双指pinch捏放，缩放操作
            float pinchRatio = LeanGesture.GetPinchRatio(fingers, WheelSensitivity); //捏放比例
                
			//判断pinchRatio是否为1，1表示没有缩放
			if (pinchRatio != 1.0f)
			{
				CameraDistanceRatio(pinchRatio); //换为z轴方向移动
			}

			//双指操作
			if (fingers.Count == 2)
			{
                //双指上下移动，调整仰角
                Vector2 heightDelta = LeanGesture.GetScreenDelta(fingers); //手指移动的屏幕距离
                
                //双指Twist旋转，绕屏幕中心与地面交点旋转
                float twistDegree = LeanGesture.GetTwistDegrees(fingers, LeanGesture.GetScreenCenter(), LeanGesture.GetLastScreenCenter());

				//双指操作
				CameraTwistAndHeight(heightDelta.y, twistDegree);

				//双指操作，调整仰角，绕屏幕中心与地面交点旋转（双指旋转时不符合功能，弃用）
				//计算屏幕中心点
//				Vector3 centerPoint;
//				float enter;
//				if (GetInterPoint(cameraMain, new Vector3(0.5F, 0.5F, 0), out centerPoint, out enter))
//				{
//					_cameraDest = enter + 0.3f;
//
//					CameraTAndH(centerPoint, heightDelta.y, twistDegree);
//				}
            }
        }
    }

    /// <summary>
    /// 摄像机平移
    /// </summary>
    /// <param name="screenDelta"></param>
    private void CameraPan(Vector2 screenDelta)
    {
		float h = -screenDelta.x * xPanSpeed * _cameraDest * Time.deltaTime * 0.001f;
		float v = -screenDelta.y * zPanSpeed * _cameraDest * Time.deltaTime * 0.001f;
        if (h != 0 || v != 0)
        {
            Vector3 targetDirection = new Vector3(h, 0, v);
            float y = cameraMain.transform.rotation.eulerAngles.y;
            targetDirection = Quaternion.Euler(0, y, 0) * targetDirection; //偏转摄像机y角度方向

            //计算屏幕中心点
            Vector3 centerPoint;
            float enter;
            if (GetInterPoint(cameraMain, new Vector3(0.5F, 0.5F, 0), out centerPoint, out enter))
            {
                //Vector3 judgePoint = transform.position + targetDirection;
                Vector3 judgePoint = centerPoint + targetDirection; //以中心点为判断点
                //print(judgePoint);

                //float offset = -transform.position.y * Mathf.Cos(20);
                float offset = 0; //偏移量，正值向外扩张，负值向里收缩
                if (judgePoint.x > xMin - offset
                   && judgePoint.x < xMax + offset
                   && judgePoint.z > zMin - offset
                   && judgePoint.z < zMax + offset)
                {
                    transform.Translate(targetDirection, Space.World);
                }
            }
        }
    }

    /// <summary>
    /// 摄像机缩放(摄像机z轴方向移动)
    /// </summary>
    /// <param name="pinchRatio"></param>
	private void CameraDistanceRatio(float pinchRatio)
    {
		//修正缩放速度，以摄像机离地高度为参考
		DistanceSpeed = _cameraDest * 1.0f;
		
        float distance = DistanceSpeed * (1 - pinchRatio); //大于0移近摄像机，小于0移远摄像机

        //distance = Mathf.Clamp(distance, DistanceMin, DistanceMax);

        Vector3 centerPoint;
        float enter;
        if (GetInterPoint(cameraMain, new Vector3(0.5F, 0.5F, 0), out centerPoint, out enter))
        {
            float dest = enter - distance;
            //Debug.Log(enter + "_" + distance);

            if (dest > DistanceMin && dest < DistanceMax )
            {
                //摄像机z轴方向移动
                transform.Translate(new Vector3(0, 0, distance), Space.Self);
                //CameraDest = dest;
				_cameraDest = dest;
            }
        }
    }

    /// <summary>
    /// 摄像机绕y轴旋转，以及仰角调整
    /// </summary>
    /// <param name="degrees"></param>
    /// <param name="delta"></param>
    private void CameraTwistAndHeight(float heightDelta, float twistDegree)
    {
        Ray ray1 = cameraMain.ViewportPointToRay(new Vector3(0.4F, 0.5F, 0)); //2条从摄像机发出的射线，与地平面相交，得到旋转轴
        Ray ray2 = cameraMain.ViewportPointToRay(new Vector3(0.6F, 0.5F, 0));

        Plane floor = new Plane(Vector3.up, Vector3.zero); //地平面

        float enter1; //射线起点与屏幕相交点距离
        float enter2;
        bool raycast1 = floor.Raycast(ray1, out enter1); //射线与地平面相交检测
        bool raycast2 = floor.Raycast(ray2, out enter2);

        if (raycast1 && raycast2)
        {
            Vector3 point1 = ray1.GetPoint(enter1); //得到相交点，point2 - point1为旋转轴
            Vector3 point2 = ray2.GetPoint(enter2);

            //print(point1);
            //print(point2);

            float degree = transform.eulerAngles.x; //当前摄像机x角
            float raise = heightDelta * SensitivityHeight; //偏转角
            if (degree + raise >= HeightAngleMin && degree + raise <= HeightAngleMax) //限制摄像机x角度
            {
                //围绕屏幕中心水平轴旋转,调整仰角
                if(twistDegree < 1 && twistDegree > -1)
                {
                	transform.RotateAround(point1, point2 - point1, raise);
                }
            }

            //计算中心点
            Vector3 centerPoint;
            float enter;
            if (GetInterPoint(cameraMain, new Vector3(0.5F, 0.5F, 0), out centerPoint, out enter))
            {
                Vector3 judgePoint = centerPoint;
                //print(judgePoint);

                float offset = 0;
                if (judgePoint.x > xMin - offset
                   && judgePoint.x < xMax + offset
                   && judgePoint.z > zMin - offset
                   && judgePoint.z < zMax + offset)
                {
                    //围绕中心点沿y轴旋转，中心点(point1 + point2) / 2
                    if(twistDegree > twistDegreeRestrict || twistDegree < -twistDegreeRestrict)
                    {
                    	transform.RotateAround(centerPoint, Vector3.up, twistDegree);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 摄像机绕y轴旋转，以及仰角调整
    /// </summary>
    /// <param name="degrees"></param>
    /// <param name="delta"></param>
    private void CameraTAndH(Vector3 center, float heightDelta, float twistDegree)
    {
		//获取当前摄像机参数
		_currRotation = cameraMain.transform.eulerAngles.y;
		//_cameraDest = enter;
		_currTilt = cameraMain.transform.eulerAngles.x;

		twistDegree = twistDegree * horizontalSpeed * Time.deltaTime;
		heightDelta = heightDelta * verticalSpeed * Time.deltaTime;

		Rotation = WrapAngle(_currRotation + twistDegree);
		Tilt = WrapAngle(_currTilt + heightDelta);

		Tilt = Mathf.Clamp(Tilt, HeightAngleMin, HeightAngleMax);
		Distance = Mathf.Clamp(_cameraDest, DistanceMin, DistanceMax);
        Rotation = Mathf.Clamp(Rotation, MinRotation, MaxRotation);

        _currRotation = Rotation;
		_currDistance = Distance;
        _currTilt = Tilt;

		//Debug.Log(_cameraDest);

		UpdateCamera(center);
    }

	/// <summary>
	/// Update the camera position and rotation based on calculated values
	/// </summary>
	private void UpdateCamera(Vector3 TargetPosition)
	{
		var rotation = Quaternion.Euler(_currTilt, _currRotation, 0);
		var v = new Vector3(0.0f, 0.0f, -_currDistance);
		var position = rotation * v + TargetPosition;

		// update position and rotation of camera
		transform.rotation = rotation;
		transform.position = position;
	}

	/// <summary>
	/// Wraps the angle.
	/// </summary>
	/// <returns>The angle.</returns>
	/// <param name="angle">Angle.</param>
    private float WrapAngle(float angle)
    {
        while (angle < -180)
        {
            angle += 360;
        }
        while (angle > 180)
        {
            angle -= 360;
        }
        return angle;
    }

    /// <summary>
    /// 获取摄像机射线与地平面的交点
    /// </summary>
    /// <param name="camera"></param>
    /// <param name="viewPoint">屏幕上的点,(0,0)到(1,1)</param>
    /// <param name="point"></param>
    /// <returns></returns>
    bool GetInterPoint(Camera camera, Vector3 viewPoint, out Vector3 point, out float enter)
    {
        point = new Vector3();

        Ray ray = cameraMain.ViewportPointToRay(viewPoint); //从摄像机发出的射线
        Plane floor = new Plane(Vector3.up, Vector3.zero); //地平面

        //float enter; //射线起点与屏幕相交点距离
        bool raycast = floor.Raycast(ray, out enter); //射线与地平面相交检测

        if (raycast)
        {
            point = ray.GetPoint(enter); //得到相交点
        }
        return raycast;
    }

	/// <summary>
	/// 获取摄像机离屏幕中心点的距离
	/// </summary>
	/// <returns>The center distance.</returns>
	float GetCenterDistance()
	{
		float ret = 0.0f;
		
		Vector3 centerPoint;
		float enter;
		if (GetInterPoint (cameraMain, new Vector3 (0.5F, 0.5F, 0), out centerPoint, out enter)) {
			ret = enter;
		}

		return ret;
	}


    /// <summary>
    /// 判断是否在偏移范围内
    /// </summary>
    /// <param name="judgePoint"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    bool JudgeRange(Vector3 judgePoint, float offset)
    {
        return judgePoint.x > xMin - offset
            && judgePoint.x < xMax + offset
            && judgePoint.z > zMin - offset
            && judgePoint.z < zMax + offset;
    }


	/// <summary>
	/// 调整摄像机仰角到某个角度
	/// </summary>
	/// <param name="altitude">调整到的角度</param>
	public void CameraToAltitude(float altitude)
	{
		altitude = Mathf.Clamp (altitude, HeightAngleMin, HeightAngleMax);

		Ray ray1 = cameraMain.ViewportPointToRay(new Vector3(0.4F, 0.5F, 0)); //2条从摄像机发出的射线，与地平面相交，得到旋转轴
		Ray ray2 = cameraMain.ViewportPointToRay(new Vector3(0.6F, 0.5F, 0));

		Plane floor = new Plane(Vector3.up, Vector3.zero); //地平面

		float enter1; //射线起点与屏幕相交点距离
		float enter2;
		bool raycast1 = floor.Raycast(ray1, out enter1); //射线与地平面相交检测
		bool raycast2 = floor.Raycast(ray2, out enter2);

		if (raycast1 && raycast2)
		{
			Vector3 point1 = ray1.GetPoint(enter1); //得到相交点，point2 - point1为旋转轴
			Vector3 point2 = ray2.GetPoint(enter2);

			//print(point1);
			//print(point2);

			float degree = transform.eulerAngles.x; //当前摄像机x角
			float raise = altitude - degree; //偏转角
			if (degree + raise >= HeightAngleMin && degree + raise <= HeightAngleMax) //限制摄像机x角度
			{
				//围绕屏幕中心水平轴旋转,调整仰角
				transform.RotateAround(point1, point2 - point1, raise);
			}
		}
	}

	/// <summary>
	/// 摄像机z轴方向移动到某个距离
	/// </summary>
	/// <param name="distance">移动到的距离</param>
	public void CameraToDistance(float distance)
	{
		distance = Mathf.Clamp (distance, DistanceMin, DistanceMax);

		float dist = distance - _cameraDest;

		//摄像机z轴方向移动
		transform.Translate (new Vector3 (0, 0, -dist), Space.Self);
		_cameraDest = distance;
	}

	/// <summary>
	/// Gets the current distance.
	/// </summary>
	/// <returns>The current distance.</returns>
	public float GetCurDistance()
	{
		return _cameraDest;
	}

    /// <summary>
	/// 鼠标右键按下时，控制视野
    /// </summary>
    void MouseRightUpdate()
    {
        if (Input.GetMouseButtonDown (1)) {
           
			//计算屏幕中心点距离
			Vector3 centerPoint;
			float enter;
			if (GetInterPoint (cameraMain, new Vector3 (0.5F, 0.5F, 0), out centerPoint, out enter)) {
				_cameraDest = enter + 0.3f;
				_cameraCenter = centerPoint;
			}

			bMouseRightDown = true;
        }

        if (Input.GetMouseButtonUp (1)) {
            bMouseRightDown = false;
        }

        if (bMouseRightDown) {
            MouseRightTour ();
        }
    }

	/// <summary>
	/// 鼠标右键，上下调整视角，左右绕中心旋转
	/// </summary>
    void MouseRightTour()
    {
        float h = horizontalSpeed * Input.GetAxis ("Mouse X");
        float v = verticalSpeed * Input.GetAxis ("Mouse Y");

        //CameraTwistAndHeight(-v, h);
		if (h != 0 || v != 0) {
			CameraTAndH(_cameraCenter, -v, h);
		}
    }

}
