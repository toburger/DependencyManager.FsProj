namespace DependencyManager.FsProj

open ProjectSystem
open FSharp.Compiler.SourceCodeServices
open Fake.DotNet

module Internals =
    open System

    /// A marker attribute to tell FCS that this assembly contains a Dependency Manager, or
    /// that a class with the attribute is a DependencyManager
    [<AttributeUsage(AttributeTargets.Assembly ||| AttributeTargets.Class , AllowMultiple = false)>]
    type DependencyManagerAttribute() =
        inherit Attribute()

    [<assembly: DependencyManagerAttribute()>]
    do ()

    type ScriptExtension = string
    type HashRLines = string seq
    type TFM = string

    [<DependencyManager>]
    /// the type _must_ take an optional output directory
    type FsProjDependencyManager(outputDir: string option) =

        let checker = FSharpChecker.Create()

        let projectController = ProjectController(checker)

        member _.Name = "FsProj Dependency Manager"
        member _.Key = "fsproj"

        member private _.ResolveDependenciesForSingleFsProj(fsproj: string) =
            DotNet.restore id fsproj

            let res =
                projectController.LoadProject fsproj ignore FSIRefs.TFM.NetCore (fun _ _ _ -> ())
                |> Async.RunSynchronously

            match res with
            | ProjectResponse.Project proj ->

                let setParams (defaults: MSBuildParams) =
                    { defaults with
                        Verbosity = Some(Quiet)
                        Targets = ["Build"] }
                MSBuild.build setParams proj.projectFileName

                let references = proj.references @ [proj.extra.TargetPath]

                (true, fsproj, references, [], [])

            | ProjectResponse.ProjectError(errorDetails) ->
                eprintfn "ERROR: %A" errorDetails
                (false, fsproj, [], [], [])

            | ProjectResponse.ProjectLoading _
            | ProjectResponse.WorkspaceLoad _ ->
                (false, fsproj, [], [], [])

        member _.FixReferences(success, references, files, includePaths) =
            let refsToWrite =
                references
                |> List.distinct
                |> List.map (sprintf "#r @\"%s\"")

            let filePath = IO.Path.ChangeExtension(IO.Path.GetTempFileName (), "fsx")
            IO.File.WriteAllLines(filePath, refsToWrite)

            (success, [], filePath::files, includePaths)

        member self.ResolveDependencies(scriptExt : ScriptExtension, packageManagerTextLines : HashRLines, tfm: TFM) : (bool * string list * string list * string list) =
            let resolvedDependencies = Seq.map self.ResolveDependenciesForSingleFsProj packageManagerTextLines

            ((true, [], [], []), resolvedDependencies)
            ||> Seq.fold (fun (successS, referencesS, filesS, includePathsS) ((success, fsproj, references, files, includePaths)) ->
                (successS && success), (referencesS @ references), (filesS @ files), (includePathsS @ includePaths))
            |> self.FixReferences
