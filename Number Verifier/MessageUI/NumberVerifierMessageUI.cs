﻿using System;
using System.Windows.Forms;

using Sdl.DesktopEditor.BasicControls;
using Sdl.DesktopEditor.EditorApi;
using Sdl.FileTypeSupport.Framework.NativeApi;
using Sdl.FileTypeSupport.Framework.BilingualApi;
using Sdl.FileTypeSupport.Framework.IntegrationApi;
using Sdl.Verification.Api;
using System.Linq;
using System.Drawing;

namespace Sdl.Community.Extended.MessageUI
{
    public partial class NumberVerifierMessageUI : UserControl, ISuggestionProvider, ISegmentChangedAware
    {
        #region Create Edit Controls

        private readonly BasicSegmentEditControl _sourceSegmentControl = new BasicSegmentEditControl();
        private readonly BasicSegmentEditControl _targetSegmentControl = new BasicSegmentEditControl();
        
        #endregion

        private bool _hasSegmentChanged = false;

        private Suggestion _suggestion;

        #region Constructor
        public NumberVerifierMessageUI(MessageEventArgs messageEventArgs, IBilingualDocument bilingualDocument, ISegment sourceSegment, ISegment targetSegment)
        {
            MessageEventArgs = messageEventArgs;
            BilingualDocument = bilingualDocument;
            SourceSegment = sourceSegment;
            TargetSegment = targetSegment;
            InitializeComponent();
            
            _sourceSegmentControl.Dock = DockStyle.Fill;
            _sourceSegmentControl.IsReadOnly = false;
            _sourceSegmentControl.ReplaceDocumentSegment(sourceSegment.Clone() as ISegment);
            panel_Source.Controls.Add(_sourceSegmentControl);
            _sourceSegmentControl.ReplaceDocumentSegment(sourceSegment);

            _targetSegmentControl.Dock = DockStyle.Fill;
            _targetSegmentControl.IsReadOnly = false;
			_targetSegmentControl.ReplaceDocumentSegment(targetSegment.Clone() as ISegment);
            panel_Target.Controls.Add(_targetSegmentControl);
            _targetSegmentControl.ReplaceDocumentSegment((ISegment)targetSegment.Clone());

            _targetSegmentControl.SegmentContentChanged += OnSegmentContentChanged;

			//set up the target and source rich box which will be used to identify the issued text(s)
			target_richTextBox.Text = targetSegment[0].ToString();
			source_richTextBox.Text = sourceSegment[0].ToString();

			_hasSegmentChanged = false;

            UpdateMessage(messageEventArgs);
        }
        #endregion

        public MessageEventArgs MessageEventArgs
        {
            get;
            private set;
        }

        public IBilingualDocument BilingualDocument
        {
            get;
            private set;
        }

        public ISegment SourceSegment
        {
            get;
            private set;
        }

        /// <summary>
        /// TargetSegment property represents the target segment.
        /// </summary>
        public ISegment TargetSegment
        {
            get;
            private set;
        }


        #region ISegmentChangedAware implementation
        /// <summary>
        /// Returns true if object was manually edited
        /// </summary>
        public bool HasSegmentChanged
        {
            get { return _hasSegmentChanged; }
        }

        /// <summary>
        /// Returns edited segment
        /// </summary>
        public ISegment EditedSegment
        {
            get { return _targetSegmentControl.GetDocumentSegment(); }
        }

        /// <summary>
        /// The paragraph unit ID for edited segment in the original document.
        /// Note: The segment may not reference the original document so the paragraph ID may be null.
        /// </summary>
        public ParagraphUnitId? TargetParagraphId
        {
            get { return MessageEventArgs.FromLocation.ParagrahUnitId; }
        }

        /// <summary>
        /// The segment ID for the edited segment in the original document.
        /// </summary>
        public SegmentId? TargetSegmentId
        {
            get { return MessageEventArgs.FromLocation.SegmentId; }
        }

        /// <summary>
        /// Reset target segment content to original value 
        /// </summary>
        public void ResetSegment()
        {
            // don't listen for events when contents are reset
            _targetSegmentControl.SegmentContentChanged -= OnSegmentContentChanged;

            // show target segment in segment control
            _targetSegmentControl.ReplaceDocumentSegment((ISegment)TargetSegment.Clone());

            _hasSegmentChanged = false;

            // start listening to changes again 
            _targetSegmentControl.SegmentContentChanged += OnSegmentContentChanged;

        }

        public event EventHandler<EventArgs> SegmentChanged;

        #endregion


        #region Private members
        /// <summary>
        /// Updates the message from the given message event arguments.
        /// </summary>
        /// <param name="messageEventArgs">message event arguments</param>
        private void UpdateMessage(MessageEventArgs messageEventArgs)
        {
            var messageData = (NumberVerifierMessageData)messageEventArgs.ExtendedData;
            tb_ErrorDetails.Text = messageEventArgs.Level.ToString() + "\r\n" + messageEventArgs.Message;
            tb_SourceIssues.Text = messageData.SourceIssues;
            tb_TargetIssues.Text = messageData.TargetIssues;
			
			UnderlineText(tb_SourceIssues.Text, tb_TargetIssues.Text);
		}

        /// <summary>
        /// Handle content changed event
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="args">Always null</param>
        private void OnSegmentContentChanged(object sender, EventArgs args)
        {
            _hasSegmentChanged = true;
            if (SegmentChanged != null)
            {
                SegmentChanged(this, null);
            }
        }

		private void UnderlineText(string sourceIssue, string targetIssue)
		{
			var sourceSplits = source_richTextBox.Text.Split(' ').ToList();
			foreach (var item in sourceSplits)
			{
				if (!string.IsNullOrEmpty(sourceIssue) &&
					!string.IsNullOrEmpty(item) 
					&& (sourceIssue.Contains(item) || item.Contains(sourceIssue)))
				{
					int indexToText = source_richTextBox.Find(item);
					int endIndex = item.Length;
					source_richTextBox.Select(indexToText, endIndex);

					source_richTextBox.SelectionColor = Color.Red;
				}
			}

			var targetSplits = target_richTextBox.Text.Split(' ').ToList();
			foreach(var item in targetSplits)
			{
				if(!string.IsNullOrEmpty(targetIssue)
					&& !string.IsNullOrEmpty(item)
					&& (targetIssue.Contains(item) || item.Contains(targetIssue)))
				{
					int indexToText = target_richTextBox.Find(item);
					int endIndex = item.Length;
					target_richTextBox.Select(indexToText, endIndex);
					target_richTextBox.SelectionBackColor = Color.Gold;
				}
			}
		}

		#endregion
		#region ISuggestionProvider
		public Suggestion GetSuggestion()
        {
            return _suggestion;
        }

        public bool HasSuggestion()
        {
            return false;
        }

        public event EventHandler SuggestionChanged;
        #endregion


    }
}
