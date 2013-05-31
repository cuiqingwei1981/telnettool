/***************************************************************************************************
 * 
 * Copyright 1998-2003 XiaoCui' Technology Co.,Ltd.
 * 
 * DESCRIPTION: 
 * 	   
 * modification history
 * --------------------
 * 01a, 12.03.2010, cuiqingwei written
 * --------------------
 **************************************************************************************************/

/*                   著作权所有(C) Edutech〈2009-2010〉〈开发者：崔庆伟〉

本程序为自由软件；您可依据自由软件基金会所发表的GNU通用公共授权条款规定，就本程序再为发布与／或修改；
无论您依据的是本授权的第二版或（您自行选择的）任一日后发行的版本。

本程序是基于使用目的而加以发布，然而不负任何担保责任；亦无对适售性或特定目的适用性所为的默示性担保。
详情请参照GNU通用公共授权。                                                                       */

#region 相关引用
using System;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
#endregion

namespace Telnet
{
    public partial class frmAbout : Form
    {
    private string TempFile = Path.GetTempFileName();

    public frmAbout()
    {
      InitializeComponent();
      
      // Read the about HTML from the assembly
      string html = (new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Telnet.About.htm"))).ReadToEnd();

      // Replace sections with appropriate data
      html = html.Replace("{version}", Assembly.GetExecutingAssembly().GetName().Version.ToString());

      // Save the temp file so the web browser has a target to navigate to
      File.WriteAllText(TempFile, html);

      //// Show the temp about file 
      web.Navigate(TempFile);
    }

    private void web_Navigated(object sender, WebBrowserNavigatedEventArgs e)
    {
      // Since the navigation is complete, delete the temp file
      File.Delete(TempFile);
    }

    private void frmAbout_Load(object sender, EventArgs e)
    {

    }
  }
}

/*-------------------------------------------------------------------------------------------------
								             	 0ooo
							           	ooo0     (   )
								        (   )     ) /
							           	 \ (     (_/
	    				                  \_)        By:cuiqingwei [gary]
--------------------------------------------------------------------------------------------------*/