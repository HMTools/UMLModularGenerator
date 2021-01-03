﻿using Octokit;
using PlantUml.Net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UMLGenerator.Interfaces;
using UMLGenerator.Models.CodeModels;
using UMLGenerator.Models.FileSystemModels;
using UMLGenerator.WPFLibrary;

namespace UMLGenerator.ViewModels
{
    public class UMLScreenViewModel : BaseViewModel
    {
        #region Commands
        public RelayCommand GenerateUMLCommand { get; private set; }
        public RelayCommand SwitchShowUMLCommand { get; private set; }
        public RelayCommand SaveSvgCommand { get; private set; }
        public RelayCommand SavePngCommand { get; private set; }
        public RelayCommand SavePlantUMLCommand { get; private set; }

        #endregion
        #region Properties
        public Dictionary<string, NamespaceModel> Namespaces { get; set; }
        public Dictionary<string, List<string>> Classes { get; set; }
        public Dictionary<string, List<string>> Interfaces { get; set; }
        private string results;

        public string Results
        {
            get { return results; }
            set { results = value; NotifyPropertyChanged(); }
        }

        private bool isUmlPreview = true;

        public bool IsUmlPreview
        {
            get { return isUmlPreview; }
            set { isUmlPreview = value; NotifyPropertyChanged(); }
        }
        public UML.ShowUmlViewModel UmlViewModel { get; set; }

        #endregion

        #region Public Static Fields
        public static Dictionary<string, char> AccessModifiersDict = new Dictionary<string, char>() //maybe need to remove from static
        {
            { "", '-'},
            { "private", '-'},
            { "protected", '#'},
            { "private protected", '#'},
            { "protected internal", '#'},
            { "internal", '#'},
            { "public", '+'}
        };
        #endregion

        #region Fields
        private MainViewModel mainVM;
        #endregion

        #region Constructors
        public UMLScreenViewModel(MainViewModel mainVM, List<FileModel> fileModels)
        {
            this.mainVM = mainVM;
            Namespaces = new Dictionary<string, NamespaceModel>();
            Classes = new Dictionary<string, List<string>>();
            Interfaces = new Dictionary<string, List<string>>();
            if(mainVM.RepostioryID == 0)
            {
                RunOnFiles(fileModels);
            }
            else
                RunOnFiles(fileModels, mainVM.GitClient, mainVM.RepostioryID);
            Results = GenerateUML(Namespaces.Values);
            UmlViewModel = new UML.ShowUmlViewModel(Results);
            AddCommands();
        }
        #endregion

        #region Methods

        private void AddCommands()
        {
            SaveSvgCommand = new RelayCommand(o =>
            {
                SaveFileDialog dialog = new SaveFileDialog() { Filter = "SVG file (*.svg)|*.svg" };
                dialog.ShowDialog();
                if(dialog.FileName != "")
                {
                    File.WriteAllText(dialog.FileName, UmlViewModel.SvgString);
                }

            }, (o) => !UmlViewModel.IsLoading);

            SavePngCommand = new RelayCommand(o =>
            {
                SaveFileDialog dialog = new SaveFileDialog() { Filter = "PNG file (*.png)|*.png" };
                dialog.ShowDialog();
                if (dialog.FileName != "")
                {
                    UmlViewModel.UMLImage.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
                }

            }, (o) => !UmlViewModel.IsLoading);

            SavePlantUMLCommand = new RelayCommand(o =>
            {
                SaveFileDialog dialog = new SaveFileDialog() { Filter = "PlantUML file (*.wsd)|*.wsd" };
                dialog.ShowDialog();
                if (dialog.FileName != "")
                {
                    File.WriteAllText(dialog.FileName, Results);
                }

            }, (o) => !string.IsNullOrWhiteSpace(Results));

            SwitchShowUMLCommand = new RelayCommand(o =>
            {
                IsUmlPreview = !IsUmlPreview;
            });

            GenerateUMLCommand = new RelayCommand((o) => 
            { 
                Results = GenerateUML(Namespaces.Values);
                UmlViewModel.UpdateUML(Results);
            });
        }
        private void RunOnFiles(List<FileModel> fileModels) //local files
        {
            foreach(var file in fileModels)
            {
                new CodeFileViewModel(file.Name, System.IO.File.ReadAllText(file.FullName), Namespaces, Classes, Interfaces);
            }
        }

        private void RunOnFiles(List<FileModel> fileModels, GitHubClient client, long repositoryId) // github files
        {
            foreach (var file in fileModels)
            {
                try
                {
                    var code = client.Repository.Content.GetAllContents(repositoryId, file.FullName).GetAwaiter().GetResult()[0].Content;
                    new CodeFileViewModel(file.Name, code, Namespaces, Classes, Interfaces);
                }
                catch
                {
                    //failed getting this file
                }
            }
        }

        private string GenerateUML(IEnumerable<IUMLTransferable> source)
        {
            string res = "@startuml\n";
            foreach (var obj in source)
            {
                res += obj.TransferToUML(0, Classes, Interfaces);
            }
            return res + "@enduml";
        }
        #endregion
    }
}
