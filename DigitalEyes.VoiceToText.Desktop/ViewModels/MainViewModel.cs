using DigitalEyes.VoiceToText.Desktop.Views;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;

namespace DigitalEyes.VoiceToText.Desktop.ViewModels
{
    class MainViewModel : BaseViewModel
    {

        Window parentControl;

        ProjectControl projectControl;
        public ProjectControl ProjectControl
        {
            get
            {
                return projectControl;
            }
            set
            {
                if (projectControl != value)
                {
                    projectControl = value;
                    RaisePropertyChanged("ProjectControl");
                }
            }
        }

        string selectedProjectName;
        public string SelectedProjectName
        {
            get
            {
                return selectedProjectName;
            }
            set
            {
                if (selectedProjectName != value)
                {
                    selectedProjectName = value;
                    RaisePropertyChanged("SelectedProjectName");
                }
            }
        }


        public SettingsViewModel SettingsVM { get; } = new SettingsViewModel();

        RelayCommand<DE_VTT_Project> loadProjectCommand;
        public RelayCommand<DE_VTT_Project> LoadProjectCommand
        {
            get
            {
                if (loadProjectCommand == null)
                {
                    loadProjectCommand = new RelayCommand<DE_VTT_Project>(doLoadProject);
                }
                return loadProjectCommand;
            }
        }

        RelayCommand addConfigCommand;
        public RelayCommand AddConfigCommand
        {
            get
            {
                if (addConfigCommand == null)
                {
                    addConfigCommand = new RelayCommand(doAddConfig);
                }
                return addConfigCommand;
            }
        }

        private void doAddConfig()
        {
            
        }

        RelayCommand<DE_VTT_Project> newProjectCommand;
        public RelayCommand<DE_VTT_Project> NewProjectCommand
        {
            get
            {
                if (newProjectCommand == null)
                {
                    newProjectCommand = new RelayCommand<DE_VTT_Project>(doNewProject);
                }
                return newProjectCommand;
            }
        }

        private void doNewProject(DE_VTT_Project obj)
        {
            var proj = new DE_VTT_Project();

            // Instantiate the dialog box
            ProjectEditWindow dlg = new ProjectEditWindow { DataContext = proj };

            // Configure the dialog box
            dlg.Owner = (Window)parentControl;

            // Open the dialog box modally 
            var rslt = dlg.ShowDialog();

            if (rslt.HasValue && rslt.Value)
            {
                Projects.Add(proj);
                RaisePropertyChanged("Projects");
                doLoadProject(proj);
                SaveProjectsFile();
            }
        }
        
        private void doLoadProject(DE_VTT_Project project)
        {
            parentControl.Dispatcher.Invoke(() =>
            {
                if (ProjectControl != null)
                {
                    ProjectControl.Dispose();
                }

                if (project == null)
                {
                    ProjectControl = null;
                    SelectedProjectName = null;
                }
                else
                {
                    var ctrl = new ProjectControl(project);
                    ProjectControl = ctrl;
                    SelectedProjectName = project.Name;
                }
            });
        }

        ObservableCollection<DE_VTT_Project> projects;
        public ObservableCollection<DE_VTT_Project> Projects
        {
            get
            {
                if (projects == null)
                {
                    projects = new ObservableCollection<DE_VTT_Project>();
                }
                return projects;
            }
            set
            {
                if (projects != value)
                {
                    projects = value;
                    RaisePropertyChanged("Projects");
                }
            }
        }

        private void LoadProjectsFile()
        {
            if (!Directory.Exists(SettingsVM.ProjectsFolder))
            {
                Directory.CreateDirectory(SettingsVM.ProjectsFolder);
            }

            try
            {
                if (!File.Exists(SettingsVM.ProjectsFile))
                {
                    SaveProjectsFile();
                    //MessageBox.Show($"File not found: {SettingsVM.ProjectsFile}");
                    return;
                }

                FileStream fs = new FileStream(SettingsVM.ProjectsFile, FileMode.Open);

                DataContractSerializer ser = new DataContractSerializer(typeof(List<DE_VTT_Project>));
                using (var reader = XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas()))
                {
                    List<DE_VTT_Project> projList = (List<DE_VTT_Project>)ser.ReadObject(reader, true);
                    Projects = new ObservableCollection<DE_VTT_Project>(projList);
                }

                if (Projects.Count == 0)
                {
                    //ShowImportSection = true;
                    MessageBox.Show("You need to create a project before you can continue");
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"{exc}");
                MessageBox.Show("Error deserialising the projects file. Has anything on the class models changed, no longer matching the json schema?");
                return;
            }

        }

        public void SaveProjectsFile()
        {
            if (!Directory.Exists(SettingsVM.ProjectsFolder))
            {
                Directory.CreateDirectory(SettingsVM.ProjectsFolder);
            }

            try
            {
                DataContractSerializer ser = new DataContractSerializer(typeof(List<DE_VTT_Project>));
                var xmlSettings = new XmlWriterSettings { Indent = true, IndentChars = "\t" };
                using (var writer = XmlWriter.Create(SettingsVM.ProjectsFile, xmlSettings))
                {
                    ser.WriteObject(writer, Projects.ToList());
                }

                if (ProjectControl != null)
                {
                    ProjectControl.ViewModel.ProgressingInfo = $"{DateTime.Now}: Saved OK";
                }
            }
            catch (Exception exc)
            {
                //SendNotificationUpdate($"{exc}");
                var msg = $"Error serialising or saving the projects file: {exc}";
                MessageBox.Show(msg);
            }
        }

        public MainViewModel(Window parentWindow)
        {
            parentControl = parentWindow;
            Messenger.Default.Register<ProjectViewModel>(this, doSaveProject);
            LoadProjectsFile();
            Messenger.Default.Register<DE_VTT_Project>(this, "delete", doDeleteProject);
        }

        private void doDeleteProject(DE_VTT_Project obj)
        {
            parentControl.Dispatcher.Invoke(() =>
            {
                Projects.Remove(obj);
                SaveProjectsFile();
                doLoadProject(null);
                RaisePropertyChanged("Projects");
            });
        }
        
        private void doSaveProject(ProjectViewModel obj)
        {
            SaveProjectsFile();
        }
    }
}
