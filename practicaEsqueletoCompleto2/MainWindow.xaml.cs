using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Kinect;

namespace practicaEsqueletoCompleto2
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor miKinect;
        Boolean Calibracion_Status=false;
        System.IO.Ports.SerialPort Arduino;

        


        byte[] datosColor = null;
        WriteableBitmap colorImagenBitmap = null;

        public MainWindow()
        {
            InitializeComponent();
            Arduino = new System.IO.Ports.SerialPort();
            Arduino.PortName = "COM3";
            Arduino.BaudRate = 9600;
            Arduino.Open();

        }
        

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            if (KinectSensor.KinectSensors.Count == 0)
            {
                MessageBox.Show("No se detecta ningun kinect");
                Application.Current.Shutdown();
            }

            miKinect = KinectSensor.KinectSensors.FirstOrDefault();

            try
            {
                miKinect.SkeletonStream.Enable();
                miKinect.ColorStream.Enable();
                miKinect.Start();
            }
            catch
            {
                MessageBox.Show("La inicializacion del Kinect fallo");
                Application.Current.Shutdown();
            }

            miKinect.SkeletonFrameReady += miKinect_SkeletonFrameReady;
            miKinect.ColorFrameReady += miKinect_ColorFrameReady;
        }

        void miKinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame framesColor = e.OpenColorImageFrame())
            {
                if (framesColor == null) return;

                if (datosColor == null)
                    datosColor = new byte[framesColor.PixelDataLength];

                framesColor.CopyPixelDataTo(datosColor);

                if (colorImagenBitmap == null)
                {
                    this.colorImagenBitmap = new WriteableBitmap(
                        framesColor.Width,
                        framesColor.Height,
                        96,
                        96,
                        PixelFormats.Bgr32,
                        null);
                }

                this.colorImagenBitmap.WritePixels(
                    new Int32Rect(0, 0, framesColor.Width, framesColor.Height),
                    datosColor,
                    framesColor.Width * framesColor.BytesPerPixel,
                    0
                    );

                canvasEsqueleto.Background = new ImageBrush(colorImagenBitmap);
            }
        }

        void miKinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            canvasEsqueleto.Children.Clear();
            Skeleton[] esqueletos = null;

            using (SkeletonFrame frameEsqueleto = e.OpenSkeletonFrame())
            {
                if (frameEsqueleto != null)
                {
                    esqueletos = new Skeleton[frameEsqueleto.SkeletonArrayLength];
                    frameEsqueleto.CopySkeletonDataTo(esqueletos);
                }
            }

            if (esqueletos == null) return;

            foreach (Skeleton esqueleto in esqueletos)
            {
                if (esqueleto.TrackingState == SkeletonTrackingState.Tracked)
                {
                    Joint handJoint = esqueleto.Joints[JointType.HandRight];
                    Joint elbowJoint = esqueleto.Joints[JointType.ElbowRight];

                    // Columna Vertebral
                    dibujarLineaColumna(esqueleto.Joints[JointType.Head], esqueleto.Joints[JointType.ShoulderCenter]);
                    dibujarLineaColumna(esqueleto.Joints[JointType.ShoulderCenter], esqueleto.Joints[JointType.Spine]);


                    /*
                    // Brazo Izquierda
                    agregarLinea(esqueleto.Joints[JointType.ShoulderCenter], esqueleto.Joints[JointType.ShoulderLeft]);
                    agregarLinea(esqueleto.Joints[JointType.ShoulderLeft], esqueleto.Joints[JointType.ElbowLeft]);
                    agregarLinea(esqueleto.Joints[JointType.ElbowLeft], esqueleto.Joints[JointType.WristLeft]);
                    agregarLinea(esqueleto.Joints[JointType.WristLeft], esqueleto.Joints[JointType.HandLeft]);
                    */
                    // Brazo Derecho
                    dibujarLineaBrazoDerecho(esqueleto.Joints[JointType.ShoulderCenter], esqueleto.Joints[JointType.ShoulderRight]);
                    dibujarLineaBrazoDerecho(esqueleto.Joints[JointType.ShoulderRight], esqueleto.Joints[JointType.ElbowRight]);
                    dibujarLineaBrazoDerecho(esqueleto.Joints[JointType.ElbowRight], esqueleto.Joints[JointType.WristRight]);
                    dibujarLineaBrazoDerecho(esqueleto.Joints[JointType.WristRight], esqueleto.Joints[JointType.HandRight]);
                }
            }
        }

        void dibujarLineaBrazoDerecho(Joint j1, Joint j2)
        {
            Line lineaHueso = new Line();
            lineaHueso.Stroke = new SolidColorBrush(Colors.Red);
            lineaHueso.StrokeThickness = 10;

            // Obtener las coordenadas de j1 (inicio del brazo derecho)
            ColorImagePoint j1P = miKinect.CoordinateMapper.MapSkeletonPointToColorPoint(j1.Position, ColorImageFormat.RgbResolution640x480Fps30);
            lineaHueso.X1 = j1P.X;
            lineaHueso.Y1 = j1P.Y;

            // Obtener las coordenadas de j2 (extremo del brazo derecho)
            ColorImagePoint j2P = miKinect.CoordinateMapper.MapSkeletonPointToColorPoint(j2.Position, ColorImageFormat.RgbResolution640x480Fps30);
            lineaHueso.X2 = j2P.X;
            lineaHueso.Y2 = j2P.Y;

            // Agregar la línea al canvas
            canvasEsqueleto.Children.Add(lineaHueso);

            // Agregar círculo en el extremo del brazo derecho (j2)
            Ellipse circuloFin = new Ellipse();
            circuloFin.Fill = new SolidColorBrush(Colors.Blue);
            circuloFin.Width = 20;
            circuloFin.Height = 20;
            circuloFin.Margin = new Thickness(j2P.X - 10, j2P.Y - 10, 0, 0);
            canvasEsqueleto.Children.Add(circuloFin);

            // Obtener la coordenada Z del extremo del brazo derecho
            float extremoBrazoDerechoZ = j2.Position.Z;
            Console.WriteLine("Coordenada Z del extremo del brazo derecho: " + extremoBrazoDerechoZ);

            txt_brazoderecho.Content = j1P.X.ToString() + " X, " + j1P.Y.ToString() + " Y " + extremoBrazoDerechoZ +" Z";

            


            if (Calibracion_Status==true) {

                if (extremoBrazoDerechoZ < 1.50) {
                    txt_movimientoZ.Content = "BRAZO HACIA ENFRENTE";
                }

                if (extremoBrazoDerechoZ > 2.18) {
                    txt_movimientoZ.Content = "BRAZO HACIA ATRAS";
                }

                if (j1P.X>500) {
                    txt_movimientoX.Content = "BRAZO HACIA DERECHA";
                    Arduino.WriteLine("1");
                }

                if (j1P.X < 250)
                {
                    txt_movimientoX.Content = "BRAZO HACIA IZQUIERDA";
                    Arduino.WriteLine("180");
                }

                if (j1P.Y > 290)
                {
                    txt_movimientoY.Content = "BRAZO HACIA ABAJO";
                }

                if (j1P.Y < 120)
                {
                    txt_movimientoY.Content = "BRAZO HACIA ARRIBA";
                }

            }

        }



        void dibujarLineaColumna(Joint j1, Joint j2)
        {
            Line lineaHueso = new Line();
            lineaHueso.Stroke = new SolidColorBrush(Colors.Green);
            lineaHueso.StrokeThickness = 15;

            ColorImagePoint j1P = miKinect.CoordinateMapper.MapSkeletonPointToColorPoint(j1.Position, ColorImageFormat.RgbResolution640x480Fps30);
            lineaHueso.X1 = j1P.X;
            lineaHueso.Y1 = j1P.Y;

            ColorImagePoint j2P = miKinect.CoordinateMapper.MapSkeletonPointToColorPoint(j2.Position, ColorImageFormat.RgbResolution640x480Fps30);
            lineaHueso.X2 = j2P.X;
            lineaHueso.Y2 = j2P.Y;

            // Agregar la línea al canvas
            canvasEsqueleto.Children.Add(lineaHueso);

            // Agregar círculo en el primer punto (j1 - inicio de la línea)
            Ellipse circuloInicio = new Ellipse();
            circuloInicio.Fill = new SolidColorBrush(Colors.Blue);
            circuloInicio.Width = 20;
            circuloInicio.Height = 20;
            circuloInicio.Margin = new Thickness(j1P.X - 10, j1P.Y - 10, 0, 0);



            // Agregar los círculos al canvas
            canvasEsqueleto.Children.Add(circuloInicio);

            // Obtener la coordenada Z del extremo del brazo derecho
            float ColumnaZ = j2.Position.Z;
            Console.WriteLine("Coordenada Z del extremo del brazo derecho: " + ColumnaZ);


            txt_CABEZA.Content = j1P.X.ToString() + " X, " + j1P.Y.ToString() + " Y " + ColumnaZ + " Z";

            if (j1P.X >=320 && j1P.X<=350 && ColumnaZ>=1.90 && ColumnaZ <= 2.10) {
                txt_Calibracion.Content = "CALIBRADO";
                txt_Calibracion.Foreground = new SolidColorBrush(Colors.Green);
                Calibracion_Status = true;
            }
            else {
                txt_Calibracion.Content = "DESCALIBRADO";
                txt_Calibracion.Foreground = new SolidColorBrush(Colors.Red);
                Calibracion_Status = false;
            }
            


        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (Arduino.IsOpen) {
            Arduino.Close();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            
        }
    }
}
