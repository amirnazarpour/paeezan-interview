# Submission checklist

The assignment document asks for a zip containing a Git repository link and an APK. The linked repository should contain the Unity project, README, a short architecture note, full AI chats with an index, and a short game capture.

| Requirement | Current state |
| --- | --- |
| Repository link | `RepositoryLink.txt` points to the configured Git remote. Commit and push the final changes before sending it. |
| Unity source | Present in `Assets/`, `Packages/`, and `ProjectSettings/`. `Library/` and `Temp/` are ignored by Git. |
| How-to-play README | Root `README.md` is ready. |
| Architecture note, at most one page | Root `ARCHITECTURE.md` is ready. |
| Full AI prompts and chats, with index | Four supplied Codex conversations have readable message exports and shared links in `ai-log/INDEX.md`. Add any other AI chats used on the project. |
| Short game screen capture | `demo/demo.mp4` is present and readable. PNGs provide additional views. |
| APK | `apk/TrickyRing v 1.0.0.apk` is present; its ZIP structure and Android manifest were checked. Install and play it on a device before submission. |
| Play from Splash | Verified in the connected `6000.3.13f1` Editor: Splash advanced to Menu. The shop EditMode suite passed 8/8 tests. |
| Unity version check | Project is authored in `6000.3.13f1`. The document names `6000.0.58f2`; that exact version has not been verified. |

Before sending, confirm that `ai-log/` covers every AI conversation used, commit and push the repository, then create a zip containing `RepositoryLink.txt` and the APK in `apk/`. The linked repository contains the Unity project, README, architecture note, AI log, and demo. Install the APK and play it on a device before submission.
