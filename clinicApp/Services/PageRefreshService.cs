using System;
using System.Windows;

namespace clinicApp.Services
{
    /// <summary>
    /// Service to notify the main window to refresh current page by re-navigating.
    /// </summary>
    public static class PageRefreshService
    {
        public static void RefreshCurrentPage()
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.RefreshCurrentPage();
            }
        }
    }
}