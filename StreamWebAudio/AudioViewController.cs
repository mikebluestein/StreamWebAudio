using System;
using System.Drawing;
using MonoTouch.AudioToolbox;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace StreamWebAudio
{
    public class AudioViewController : UIViewController
    {
        // Classical music from Vermont Public Radio Classical - http://digital.vpr.net/programs/vpr-classical
        readonly string audioUrl = "http://vprclassical.streamguys.net/vprclassical128.mp3";
        bool outputQueueStarted;
        AudioFileStream audioFileStream;
        OutputAudioQueue outputQueue;
        UIImageView logo;

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad ();

            View.BackgroundColor = UIColor.White;

            using (var image = UIImage.FromFile ("XamarinLogo.png")) {
                logo = new UIImageView (image) {
                    Frame = new RectangleF (
                        new PointF (View.Center.X - image.Size.Width / 2, View.Center.Y - image.Size.Height / 2), 
                        image.Size)
                };
                Add (logo);
            }
                  
            audioFileStream = new AudioFileStream (AudioFileType.MP3);
            audioFileStream.PacketDecoded += OnPacketDecoded;
            audioFileStream.PropertyFound += OnPropertyFound;

            GetAudio ();
        }

        void GetAudio ()
        {
            var s = NSUrlSession.FromConfiguration (
                        NSUrlSessionConfiguration.DefaultSessionConfiguration, 
                        new SessionDelegate (audioFileStream), 
                        NSOperationQueue.MainQueue);

            var dataTask = s.CreateDataTask (new NSUrl (audioUrl));

            dataTask.Resume ();
        }

        class SessionDelegate : NSUrlSessionDataDelegate
        {
            readonly AudioFileStream audioFileStream;

            public SessionDelegate (AudioFileStream audioFileStream)
            {
                this.audioFileStream = audioFileStream;
            }

            public override void DidReceiveData (NSUrlSession session, NSUrlSessionDataTask dataTask, NSData data)
            {
                audioFileStream.ParseBytes ((int)data.Length, data.Bytes, false);
            }
        }

        void OnPacketDecoded (object sender, PacketReceivedEventArgs e)
        {
            IntPtr outBuffer;
            outputQueue.AllocateBuffer (e.Bytes, out outBuffer);
            AudioQueue.FillAudioData (outBuffer, 0, e.InputData, 0, e.Bytes);
            outputQueue.EnqueueBuffer (outBuffer, e.Bytes, e.PacketDescriptions);

            if (!outputQueueStarted) {
                var status = outputQueue.Start ();
                if (status != AudioQueueStatus.Ok) {
                    Console.WriteLine ("could not start audio queue");
                }
                outputQueueStarted = true;
            }
        }

        void OnPropertyFound (object sender, PropertyFoundEventArgs e)
        {
            if (e.Property == AudioFileStreamProperty.ReadyToProducePackets) {
                outputQueue = new OutputAudioQueue (audioFileStream.StreamBasicDescription);
                outputQueue.OutputCompleted += OnOutputQueueOutputCompleted;
            }
        }

        void OnOutputQueueOutputCompleted (object sender, OutputCompletedEventArgs e)
        {
            outputQueue.FreeBuffer (e.IntPtrBuffer);
        }
    }
}