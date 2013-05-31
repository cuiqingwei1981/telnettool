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

namespace Telnet
{
    using System;

    public class buttonItem
    {
        public int buttonID;
        public string buttonTitle;
        public string isCopyOrSend;
        public string isHexOrAscii;
        public string txString;
    }

    public class buttonItemCollection
    {
        public buttonItem[] buttonItems;
    }

    internal class automationConfig
    {
    }
}

/*-------------------------------------------------------------------------------------------------
								             	 0ooo
							           	ooo0     (   )
								        (   )     ) /
							           	 \ (     (_/
	    				                  \_)        By:cuiqingwei [gary]
--------------------------------------------------------------------------------------------------*/