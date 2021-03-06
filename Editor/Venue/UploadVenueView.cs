using System;
using System.IO;
using System.Linq;
using ClusterVR.CreatorKit.Editor.Core;
using ClusterVR.CreatorKit.Editor.Core.Venue;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace ClusterVR.CreatorKit.Editor.Venue
{
    public class UploadVenueView
    {
        readonly UserInfo userInfo;
        readonly Core.Venue.Json.Venue venue;
        string worldDetailUrl;
        readonly string worldManagementUrl;

        bool executeUpload;
        string errorMessage;
        UploadVenueService currentUploadService;
        ImageView thumbnail;

        public UploadVenueView(UserInfo userInfo, Core.Venue.Json.Venue venue, ImageView thumbnail)
        {
            Assert.IsNotNull(venue);
            this.userInfo = userInfo;
            this.venue = venue;
            this.thumbnail = thumbnail;
            worldDetailUrl = venue.WorldDetailUrl;
            worldManagementUrl = ClusterVR.CreatorKit.Editor.Core.Constants.WebBaseUrl + "/account/worlds";
        }

        public VisualElement CreateView()
        {
            return new IMGUIContainer(() => {Process(); DrawUI();});
        }

        void Process()
        {
            if (executeUpload)
            {
                executeUpload = false;
                currentUploadService = null;

                if (!VenueValidator.ValidateVenue(out errorMessage))
                {
                    Debug.LogError(errorMessage);
                    EditorUtility.DisplayDialog("Cluster Creator Kit", errorMessage, "閉じる");
                    return;
                }

                ItemIdAssigner.AssignItemId();
                ItemTemplateIdAssigner.Execute();
                LayerCorrector.CorrectLayer();

                try
                {
                    AssetExporter.ExportCurrentSceneResource(venue.VenueId.Value, false); //Notice UnityPackage が大きくなりすぎてあげれないので一旦やめる
                }
                catch (Exception e)
                {
                    errorMessage = $"現在のSceneのUnityPackage作成時にエラーが発生しました。 {e.Message}";
                    return;
                }

                currentUploadService = new UploadVenueService(
                    userInfo.VerifiedToken,
                    venue,
                    completionResponse =>
                    {
                        errorMessage = "";
                        worldDetailUrl = completionResponse.Url;
                        if (EditorPrefsUtils.OpenWorldManagementPageAfterUpload)
                        {
                            Application.OpenURL(worldManagementUrl);
                        }
                    },
                    exception =>
                    {
                        Debug.LogException(exception);
                        errorMessage = $"ワールドのアップロードに失敗しました。時間をあけてリトライしてみてください。";
                        EditorWindow.GetWindow<VenueUploadWindow>().Repaint();
                    });
                currentUploadService.Run();
                errorMessage = null;
            }
        }

        void DrawUI()
        {
            EditorGUILayout.Space();
            EditorPrefsUtils.OpenWorldManagementPageAfterUpload = EditorGUILayout.ToggleLeft("アップロード後にワールド管理ページを開く", EditorPrefsUtils.OpenWorldManagementPageAfterUpload);
            EditorGUILayout.HelpBox("アップロードするシーンを開いておいてください。", MessageType.Info);

            if (thumbnail.IsEmpty)
            {
                EditorGUILayout.HelpBox("サムネイル画像を設定してください。", MessageType.Error);
            }

            using (new EditorGUI.DisabledScope(thumbnail.IsEmpty))
            {
                var uploadButton = GUILayout.Button($"'{venue.Name}'としてアップロードする");
                if (uploadButton)
                {
                    executeUpload = EditorUtility.DisplayDialog(
                        "ワールドをアップロードする",
                        $"'{venue.Name}'としてアップロードします。よろしいですか？",
                        "アップロード",
                        "キャンセル"
                    );
                }
            }

            if (GUILayout.Button("ワールド管理ページを開く"))
            {
                Application.OpenURL(worldManagementUrl);
            }

            EditorGUILayout.Space();

            if (!string.IsNullOrEmpty(errorMessage))
            {
                EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
            }

            if (currentUploadService == null)
            {
                return;
            }

            if (!currentUploadService.IsProcessing)
            {
                EditorUtility.ClearProgressBar();
                foreach (var status in currentUploadService.UploadStatus)
                {
                    var text = status.Value ? "Success" : "Failed";
                    EditorGUILayout.LabelField(status.Key.ToString(), text);
                }
            }
            else
            {
                var statesValue = currentUploadService.UploadStatus.Values.ToList();
                var finishedProcessCount = statesValue.Count(x => x);
                var allProcessCount = statesValue.Count;
                EditorUtility.DisplayProgressBar(
                    "Venue Upload",
                    $"upload processing {finishedProcessCount} of {allProcessCount}",
                    (float) finishedProcessCount / allProcessCount
                );
            }

            if (!currentUploadService.IsProcessing
                && currentUploadService.UploadStatus.Values.Any(x => !x))
            {
                if (GUILayout.Button("アップロードリトライ"))
                {
                    currentUploadService.Run();
                    errorMessage = null;
                }
            }

            EditorGUILayout.Space();

            if (File.Exists(EditorPrefsUtils.LastBuildWin))
            {
                var fileInfo = new FileInfo(EditorPrefsUtils.LastBuildWin);
                EditorGUILayout.LabelField("Windowsサイズ", $"{(double) fileInfo.Length / (1024 * 1024):F2} MB"); // Byte => MByte
            }

            if (File.Exists(EditorPrefsUtils.LastBuildMac))
            {
                var fileInfo = new FileInfo(EditorPrefsUtils.LastBuildMac);
                EditorGUILayout.LabelField("Macサイズ",$"{(double) fileInfo.Length / (1024 * 1024):F2} MB"); // Byte => MByte
            }

            if (File.Exists(EditorPrefsUtils.LastBuildAndroid))
            {
                var fileInfo = new FileInfo(EditorPrefsUtils.LastBuildAndroid);
                EditorGUILayout.LabelField("Androidサイズ",$"{(double) fileInfo.Length / (1024 * 1024):F2} MB"); // Byte => MByte
            }

            if (File.Exists(EditorPrefsUtils.LastBuildIOS))
            {
                var fileInfo = new FileInfo(EditorPrefsUtils.LastBuildIOS);
                EditorGUILayout.LabelField("iOSサイズ",$"{(double) fileInfo.Length / (1024 * 1024):F2} MB"); // Byte => MByte
            }
        }
    }
}
