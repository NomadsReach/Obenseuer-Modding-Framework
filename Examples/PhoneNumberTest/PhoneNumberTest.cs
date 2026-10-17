using System;
using System.Collections.Generic;
using BepInEx;
#if BEPINEX6
using BepInEx.Unity.Mono;
#endif
using ObenseuerCore;

[BepInPlugin("example.phonenumbertest", "PhoneNumberTest", "0.1.0")]
#if BEPINEX5
[BepInDependency("obenseuer.moddingframework", "0.11.0")]
#else
[BepInDependency("obenseuer.moddingframework", ">=0.11.0")]
#endif
public class PhoneNumberTest : BaseUnityPlugin
{
    private const string ModId = "example.phonenumbertest";
    private const string Number = "555333";
    private static readonly CoreLog Log = CoreLog.For("PhoneNumberTest");

    private void Awake()
    {
        Events.WhenReady(Install);
    }

    private void Install()
    {
        ConversationDefinition conversation = new ConversationDefinition();
        conversation.Key = "phone-test";
        ConversationLine hello = new ConversationLine();
        hello.Key = "hello";
        hello.Text = "Hello? This number came from a mod.";
        conversation.Lines.Add(hello);
        ConversationLine bye = new ConversationLine();
        bye.Key = "bye";
        bye.Text = "Nothing here is saved, so the mod adds the number again every start.";
        conversation.Lines.Add(bye);

        ConversationRefusal conversationRefusal;
        string title = DialogueAuthoring.Add(ModId, conversation, out conversationRefusal);
        if (title == null)
        {
            Log.Warn("the conversation was refused: " + conversationRefusal);
            return;
        }

        PhoneRefusal refusal;
        PhoneContact contact = Phones.Add(ModId, Number, PhoneNetwork.Obenseuer, 1f, title, out refusal);
        if (contact == null)
        {
            Log.Warn("the number was refused: " + refusal);
            return;
        }

        Log.Info("dial " + contact.Number + " on the Obenseuer network to reach " + title);
    }
}
