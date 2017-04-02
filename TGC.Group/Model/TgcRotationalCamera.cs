using Microsoft.DirectX;
using TGC.Core.Camara;
using TGC.Core.Input;
using TGC.Core.Utils;

namespace TGC.Group.Model
{
    /// <summary>
    ///     Camara que permite rotar y hacer zoom alrededor de un objeto central
    /// </summary>
    public class TgcRotationalCamera : TgcCamera
    {
        public static readonly float DEFAULT_CAMERA_DISTANCE = 10f;
        public static readonly Vector3 DEFAULT_DOWN = new Vector3(0f, -1f, 0f);
        public static readonly float DEFAULT_ROTATION_SPEED = 100f;
        public static readonly float DEFAULT_ZOOM_FACTOR = 0.15f;

        /// <summary>
        ///     Crea camara con valores por defecto.
        /// </summary>
        public TgcRotationalCamera(TgcD3dInput input)
        {
            Input = input;
            CameraCenter = new Vector3(0, 0, 0);
            NextPos = new Vector3(0, 0, 0);
            CameraDistance = DEFAULT_CAMERA_DISTANCE;
            ZoomFactor = DEFAULT_ZOOM_FACTOR;
            RotationSpeed = DEFAULT_ROTATION_SPEED;
            DiffX = 0f;
            DiffY = 0f;
            DiffZ = 1f;
            PanSpeed = 0.01f;
            UpVector = new Vector3(0f, 1f, 0f);
            base.SetCamera(NextPos, LookAt, UpVector);
        }

        /// <summary>
        ///     Crea una camara con una posicion inicial y un objetivo.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="target"></param>
        public TgcRotationalCamera(Vector3 position, Vector3 target, TgcD3dInput input) : this(input)
        {
            NextPos = position;
            CameraCenter = target;
            base.SetCamera(NextPos, LookAt, UpVector);
        }

        /// <summary>
        ///     Crea una camara con el centro de la camara, la distancia, la velocidad de zoom, Crea una camara con el centro de la
        ///     camara, la distancia y la velocidad de rotacion.
        /// </summary>
        /// <param name="cameraCenter"></param>
        /// <param name="cameraDistance"></param>
        /// <param name="zoomFactor"></param>
        /// <param name="rotationSpeed"></param>
        public TgcRotationalCamera(Vector3 cameraCenter, float cameraDistance, float zoomFactor, float rotationSpeed,
            TgcD3dInput input)
            : this(input)
        {
            CameraCenter = cameraCenter;
            CameraDistance = cameraDistance;
            ZoomFactor = zoomFactor;
            RotationSpeed = rotationSpeed;
        }

        /// <summary>
        ///     Crea una camara con el centro de la camara, la distancia y la velocidad de zoom
        /// </summary>
        /// <param name="cameraCenter"></param>
        /// <param name="cameraDistance"></param>
        /// <param name="zoomFactor"></param>
        public TgcRotationalCamera(Vector3 cameraCenter, float cameraDistance, float zoomFactor, TgcD3dInput input) :
            this(cameraCenter, cameraDistance, zoomFactor, DEFAULT_ROTATION_SPEED, input)
        {
        }

        /// <summary>
        ///     Crea una camara con el centro de la camara, la distancia
        /// </summary>
        /// <param name="cameraCenter"></param>
        /// <param name="cameraDistance"></param>
        public TgcRotationalCamera(Vector3 cameraCenter, float cameraDistance, TgcD3dInput input) :
            this(cameraCenter, cameraDistance, DEFAULT_ZOOM_FACTOR, input)
        {
        }

        /// <summary>
        ///     Centro de la camara sobre la cual se rota
        /// </summary>
        public Vector3 CameraCenter { get; set; }

        /// <summary>
        ///     Distance entre la camara y el centro
        /// </summary>
        public float CameraDistance { get; set; }

        public float DiffX { get; set; }
        public float DiffY { get; set; }
        public float DiffZ { get; set; }

        public Vector3 NextPos { get; set; }

        /// <summary>
        ///     Velocidad de paneo
        /// </summary>
        public float PanSpeed { get; set; }

        /// <summary>
        ///     Velocidad de rotacion de la camara
        /// </summary>
        public float RotationSpeed { get; set; }

        /// <summary>
        ///     Velocidad con la que se hace Zoom
        /// </summary>
        public float ZoomFactor { get; set; }

        private TgcD3dInput Input { get; }

        public override void SetCamera(Vector3 position, Vector3 target)
        {
            NextPos = position;
            CameraCenter = target;
            base.SetCamera(NextPos, CameraCenter, UpVector);
        }

        /// <summary>
        ///     Actualiza los valores de la camara.
        /// </summary>
        /// <param name="elapsedTime">Tiempo transcurrido desde el frame anterior</param>
        public override void UpdateCamera(float elapsedTime)
        {
            // Obtener variacion XY del mouse
            var mouseX = 0f;
            var mouseY = 0f;
            if (Input.buttonDown(TgcD3dInput.MouseButtons.BUTTON_LEFT))
            {
                mouseX = Input.XposRelative;
                mouseY = Input.YposRelative;

                DiffX += mouseX * elapsedTime * RotationSpeed;
                DiffY += mouseY * elapsedTime * RotationSpeed;
            }
            else
            {
                DiffX += mouseX;
                DiffY += mouseY;
            }

            // Calcular rotacion a aplicar
            var rotX = -DiffY / FastMath.PI;
            var rotY = DiffX / FastMath.PI;

            // Truncar valores de rotacion fuera de rango
            if (rotX > FastMath.PI * 2 || rotX < -FastMath.PI * 2)
            {
                DiffY = 0;
                rotX = 0;
            }

            // Invertir Y de UpVector segun el angulo de rotacion
            UpVector = rotX < -FastMath.PI / 2 && rotX > -FastMath.PI * 3 / 2 ? DEFAULT_DOWN : rotX > FastMath.PI / 2 && rotX < FastMath.PI * 3 / 2 ? DEFAULT_DOWN : DEFAULT_UP_VECTOR;

            // Determinar distancia de la camara o zoom segun el Mouse Wheel
            if (Input.WheelPos != 0)
            {
                DiffZ += ZoomFactor * Input.WheelPos * -1;
            }
            var distance = -CameraDistance * DiffZ;

            // Limitar el zoom a 0
            if (distance > 0)
            {
                distance = 0;
            }

            // Realizar Transformacion: primero alejarse en Z, despues rotar en X e Y y despues ir al centro de la cmara
            var m = Matrix.Translation(0, 0, -distance)
                    * Matrix.RotationX(rotX)
                    * Matrix.RotationY(rotY)
                    * Matrix.Translation(CameraCenter);

            // Extraer la posicion final de la matriz de transformacion
            NextPos = new Vector3(m.M41, m.M42, m.M43);

            // Hacer efecto de Pan View
            if (Input.buttonDown(TgcD3dInput.MouseButtons.BUTTON_RIGHT))
            {
                var dx = -Input.XposRelative;
                var dy = Input.YposRelative;
                var panSpeedZoom = PanSpeed * FastMath.Abs(distance);

                var d = CameraCenter - NextPos;
                d.Normalize();

                var n = Vector3.Cross(d, UpVector);
                n.Normalize();

                var up = Vector3.Cross(n, d);
                var desf = Vector3.Scale(up, dy * panSpeedZoom) - Vector3.Scale(n, dx * panSpeedZoom);
                NextPos = NextPos + desf;
                CameraCenter = CameraCenter + desf;
            }

            // Asigna las posiciones de la camara.
            base.SetCamera(NextPos, CameraCenter, UpVector);
        }
    }
}