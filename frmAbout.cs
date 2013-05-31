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

/*                   ����Ȩ����(C) Edutech��2009-2010���������ߣ�����ΰ��

������Ϊ������������������������������������GNUͨ�ù�����Ȩ����涨���ͱ�������Ϊ�����룯���޸ģ�
���������ݵ��Ǳ���Ȩ�ĵڶ����������ѡ��ģ���һ�պ��еİ汾��

�������ǻ���ʹ��Ŀ�Ķ����Է�����Ȼ�������κε������Σ����޶������Ի��ض�Ŀ����������Ϊ��Ĭʾ�Ե�����
���������GNUͨ�ù�����Ȩ��                                                                       */

#region �������
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