﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LameNetHook
{
    public abstract class PageResponse
    {
        public string URL { get; protected set; }

        public PageResponse(string URL, bool register = true)
        {
            this.URL = URL;

            if(register)
                Master.addFuntionToServer(URL, getContents);
        }

        protected void removeFromServer()
        {
            Master.removeFunctionFromServer(URL);
        }

        protected abstract string getContents(SessionData sessionData);
    }

    public static class InstantPageResponse
    {
        private static System.Threading.Mutex mutex = new System.Threading.Mutex();
        private static List<string> oneTimePageResponses = new List<string>();

        /// <summary>
        /// the maximum amount of oneTimePageResponses. if the count of them exceeds this number the fist ones will be removed. 0 for no limit.
        /// </summary>
        public static int maximumOneTimePageResponses = 50;

        /// <summary>
        /// adds a page to the server, that executes the given code
        /// </summary>
        public static void addInstantPageResponse(string URL, Master.getContents code)
        {
            Master.addFuntionToServer(URL, code);
        }

        /// <summary>
        /// adds a temporary page to the server, that executes the given code (only available for ONE request)
        /// </summary>
        /// <param name="code">the code to execute</param>
        /// <returns>the name at which this temporary page will be available at.</returns>
        public static string addOneTimeInstantPageResponse(Master.getContents code)
        {
            string hash = SessionContainer.generateHash();

            mutex.WaitOne();

            if(maximumOneTimePageResponses > 0)
            {
                if (oneTimePageResponses.Count + 1 > maximumOneTimePageResponses)
                    oneTimePageResponses.RemoveAt(0);
            }

            oneTimePageResponses.Add(hash);

            mutex.ReleaseMutex();

            Master.addFuntionToServer(hash, (SessionData sessionData) => 
                {
                    Master.removeFunctionFromServer(hash);
                    return code.Invoke(sessionData);
                });

            return "/" + hash;
        }

        /// <summary>
        /// adds a page to the server, that redirects to "destinationURL"
        /// </summary>
        public static void addRedirect(string originURL, string destinationURL)
        {
            addInstantPageResponse(originURL, (SessionData sessionData) => 
                {
                    return generateRedirectCode(destinationURL, sessionData);
                });
        }

        /// <summary>
        /// adds a page to the server, that redirects to "destinationURLifTRUE" if the conditional code returns true and redirects to "destinationURLifFALSE" if the conditional code returns false
        /// </summary>
        public static void addConditionalRedirect(string originalURL, string destinationURLifTRUE, string destinationURLifFALSE, Func<SessionData, bool> conditionalCode)
        {
            addInstantPageResponse(originalURL, (SessionData sessionData) =>
                {
                    if (conditionalCode(sessionData))
                        return generateRedirectCode(destinationURLifTRUE, sessionData);

                    return generateRedirectCode(destinationURLifFALSE, sessionData);
                });
        }

        /// <summary>
        /// adds a page to the server, that redirects if the conditional code returns true and executes other code if the conditional code returns false
        /// </summary>
        public static void addRedirectOrCode(string originalURL, string destinationURLifTRUE, Master.getContents codeIfFALSE, Func<SessionData, bool> conditionalCode)
        {
            addInstantPageResponse(originalURL, (SessionData sessionData) =>
            {
                if (conditionalCode(sessionData))
                    return generateRedirectCode(destinationURLifTRUE, sessionData);

                return codeIfFALSE(sessionData);
            });
        }

        /// <summary>
        /// adds a temporary page to the server, that redirects to "destinationURL" (only available for ONE request)
        /// </summary>
        /// <returns>the name at which this temporary page will be available at.</returns>
        public static string addOneTimeRedirect(string destinationURL)
        {
            return addOneTimeInstantPageResponse((SessionData sessionData) => 
                {
                    return generateRedirectCode(destinationURL, sessionData);
                });
        }

        /// <summary>
        /// adds a temporary page to the server, that redirects to "destinationURLifTRUE" if the conditional code returns true and redirects to "destinationURLifFALSE" if the conditional code returns false (only available for ONE request)
        /// </summary>
        /// <returns>the name at which this temporary page will be available at.</returns>
        public static string addOneTimeConditionalRedirect(string destinationURLifTRUE, string destinationURLifFALSE, Func<SessionData, bool> conditionalCode)
        {
            return addOneTimeInstantPageResponse((SessionData sessionData) =>
                {
                    if (conditionalCode(sessionData))
                        return generateRedirectCode(destinationURLifTRUE, sessionData);

                    return generateRedirectCode(destinationURLifFALSE, sessionData);
                });
        }

        /// <summary>
        /// adds a temporary page to the server, that redirects if the conditional code returns true and executes other code if the conditional code returns false (only available for ONE request)
        /// </summary>
        /// <returns>the name at which this temporary page will be available at.</returns>
        public static string addOneTimeRedirectOrCode(string destinationURLifTRUE, Master.getContents codeIfFALSE, Func<SessionData, bool> conditionalCode)
        {
            return addOneTimeInstantPageResponse((SessionData sessionData) =>
            {
                if (conditionalCode(sessionData))
                    return generateRedirectCode(destinationURLifTRUE, sessionData);

                return codeIfFALSE(sessionData);
            });
        }

        public static string generateRedirectCode(string destinationURL, SessionData sessionData = null)
        {
            if (sessionData == null)
            {
                return "<head><meta http-equiv=\"refresh\" content=\"1; url = "
                               + destinationURL + "\"><script type=\"text/javascript\">window.location.href = \""
                               + destinationURL + "\"</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:5;color:#FDCD48;'><p style='overflow:overlay;}</style></head><body style='background-color:#f0f0f0;background-image: url(\"/server/error.png\");background-repeat:repeat;background-size:125px;'><div style='font-family:\"Segoe UI\",sans-serif;width:70%;max-width:800px;margin:5em auto;padding:50px;background-color:#fff;border-radius: 1em;padding-top:22px;padding-bottom:22px;border:solid;border-color:#FDD248;border-width:1;'><h1>Page Redirection</h1><hr><p>If you are not redirected automatically, follow this <a href='"
                               + destinationURL + "'>link.</a></p><p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>";
            }
            else
            {
                return "<head><script type=\"text/javascript\">onload = function(){var f=document.createElement('form');f.setAttribute('method','POST');f.setAttribute('action','"
                                + destinationURL +
                                "');f.setAttribute('enctype','text/html');var i=document.createElement('input');i.setAttribute('type','hidden');i.setAttribute('name','ssid');i.setAttribute('value','" 
                                + sessionData.ssid +
                                "');f.appendChild(i);document.body.appendChild(f);f.submit();document.body.remove(f);}</script><title>Page Redirection</title><style type=\"text/css\">hr{border:solid;border-width:5;color:#FDCD48;'><p style='overflow:overlay;}</style></head><body style='background-color:#f0f0f0;background-image: url(\"/server/error.png\");background-repeat:repeat;background-size:125px;'><div style='font-family:\"Segoe UI\",sans-serif;width:70%;max-width:800px;margin:5em auto;padding:50px;background-color:#fff;border-radius: 1em;padding-top:22px;padding-bottom:22px;border:solid;border-color:#FDD248;border-width:1;'><h1>Page Redirection</h1><hr><p>If you are not redirected automatically, follow this <a href='#' onclick=\"var f=document.createElement('form');f.setAttribute('method','POST');f.setAttribute('action','" 
                                + destinationURL +
                                "');f.setAttribute('enctype','text/html');var i=document.createElement('input');i.setAttribute('type','hidden');i.setAttribute('name','ssid');i.setAttribute('value','" 
                                + sessionData.ssid +
                                "');f.appendChild(i);document.body.appendChild(f);f.submit();document.body.remove(f);\">link.</a></p><p style='text-align:right'>- LamestWebserver (LameOS)</p></div></body>";
            }
        }
    }
}
