using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;
using Yarn.Compiler;
using Yarn.GodotIntegration.Editor;

namespace Yarn.GodotIntegration
{
    [Tool]
    public class YarnProject : Resource //, IYarnErrorSource
    {
        /// <summary>
        /// Indicates whether the last time this file was imported, the
        /// file contained lines that did not have a line tag (and
        /// therefore were assigned an automatically-generated, 'implicit'
        /// string tag.) 
        /// </summary>
        [Export] public bool LastImportHadImplicitStringIDs;

        /// <summary>
        /// Indicates whether the last time this file was imported, the
        /// file contained any string tags.
        /// </summary>
        [Export] public bool LastImportHadAnyStrings;
        /// <summary>
        /// Indicates whether the last time this file was imported, the
        /// file was able to be parsed without errors. 
        /// </summary>
        /// <remarks>
        /// This value only represents whether syntactic errors exist or
        /// not. Other errors may exist that prevent this script from being
        /// compiled into a full program.
        /// </remarks>
        [Export] public bool IsSuccessfullyParsed = false;
        public byte[] CompiledYarnProgram => Convert.FromBase64String(CompiledYarnProgramBase64);

        [Export] public string CompiledYarnProgramBase64;
        // TODO: filter scripts by parse errors
        public List<Resource> ScriptsWithParseErrors => new List<Resource>();

        //IList<string> IYarnErrorSource.CompileErrors => ParseErrorMessages;
        public bool Destroyed => false; // not sure when this is used yet
        //[Export] 
        public Localization baseLocalization;

        // [Export]
        public List<Localization> localizations = new List<Localization>();

        public LineMetadata lineMetadata;

        public LocalizationType localizationType;

        /// <summary>
        /// JSON-serialized array of <see cref="Yarn.Compiler.Diagnostic"/> objects.
        /// </summary>
        [Export] public string ProjectErrors = "[]";

        public List<SerializedDeclaration> SerializedDeclarations = new List<SerializedDeclaration>();

        [Export][Language]
        public string defaultLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;

        public List<LanguageToSourceAsset> languagesToSourceAssets = new List<LanguageToSourceAsset>();

        private Godot.Collections.Array<Resource> _sourceScripts;
        [Export] public Godot.Collections.Array<Resource> SourceScripts
        {
            get {
                return _sourceScripts;
            }
            set {
                if (_sourceScripts == value) return;
                _sourceScripts = value;
                if (Engine.EditorHint)
                {
                    #if TOOLS
                    GD.Print($"Re-compiling yarns scripts on project {ResourceName}.");
                    var projectUtility = new YarnProjectUtility();
                    projectUtility.UpdateYarnProject(this);
                    #endif
                }
            }
        }

        public List<YarnProjectError> CompileErrors => ProjectErrors == null ?
            new List<YarnProjectError>() :
            JsonConvert.DeserializeObject<List<YarnProjectError>>(ProjectErrors);

        /// <summary>
        /// Gets a value indicating whether this Yarn Project is able to
        /// generate a strings table - that is, it has no compile errors,
        /// it has at least one script, and all scripts are fully tagged.
        /// </summary>
        /// <inheritdoc path="exception"
        /// cref="GetScriptHasLineTags(Resource)"/>
        public bool CanGenerateStringsTable => this.CompileErrors.Count == 0 && SourceScripts.Count > 0 && SourceScripts.All(s => GetScriptHasLineTags(s));

        /// <summary>
        /// Gets a value indicating whether the source script has line
        /// tags.
        /// </summary>
        /// <param name="script">The source script to add. This script must
        /// have been imported by a <see cref="YarnImporter"/>.</param>
        /// <returns>
        /// <see langword="true"/> if the the script is fully tagged, <see
        /// langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when <paramref name="script"/> is <see
        /// langword="null"/>.
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Thrown when <paramref name="script"/> is not imported by a <see
        /// cref="YarnImporter"/>.
        /// </exception>
        private bool GetScriptHasLineTags(Resource script)
        {
            if (script == null)
            {
                // This might be a 'None' or 'Missing' asset, so return
                // false here.
                return false;
            }

            GD.Print("TODO: accurate check on which  scripts have line tags");
            return false;
        }

        /// <summary>
        /// The cached result of deserializing <see
        /// cref="CompiledYarnProgram"/>.
        /// </summary>
        private Program cachedProgram = null;

        /// <summary>
        /// The names of assemblies that <see cref="ActionManager"/> should look
        /// for commands and functions in when this project is loaded into a
        /// <see cref="DialogueRunner"/>.
        /// </summary>
        public List<string> searchAssembliesForActions = new List<string>();

        public Localization GetLocalization(string localeCode)
        {

            // If localeCode is null, we use the base localization.
            if (localeCode == null)
            {
                return baseLocalization;
            }

            foreach (var loc in localizations)
            {
                if (loc.LocaleCode == localeCode)
                {
                    return loc;
                }
            }

            // We didn't find a localization. Fall back to the Base
            // localization.
            return baseLocalization;
        }

        /// <summary>
        /// Gets the Yarn Program stored in this project.
        /// </summary>
        [System.Obsolete("Use the Program property instead, which caches its return value.")]
        public Program GetProgram()
        {
            return Program.Parser.ParseFrom(CompiledYarnProgram);
        }

        /// <summary>
        /// Gets the Yarn Program stored in this project.
        /// </summary>
        /// <remarks>
        /// The first time this is called, the program stored in <see
        /// cref="CompiledYarnProgram"/> is deserialized and cached. Future
        /// calls to this method will return the cached value.
        /// </remarks>
        public Program Program
        {
            get {
                if (cachedProgram == null)
                {
                    cachedProgram = Program.Parser.ParseFrom(CompiledYarnProgram);
                }
                return cachedProgram;
            }
        }
    }

    public enum LocalizationType
    {
        YarnInternal,
        Unity,
    }
}