using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dargon.Courier.ManagementTier;

namespace Views {
   public class MobsTreeView : TreeView {
      private readonly TrieNode rootTrieNode;

      public MobsTreeView() {
         rootTrieNode = new TrieNode {
            WinformsNode = null,
            Text = null,
         };

         this.NodeMouseClick += (s, e) => HandleNodeSelectionChanged();
         this.NodeMouseDoubleClick += (s, e) => HandleNodeOpenRequested();
         this.KeyDown += (s, e) => {
            if (e.KeyCode == Keys.Enter) {
               HandleNodeOpenRequested();
               this.Focus();
            }
         };
         this.KeyPress += (s, e) => {
            if (e.KeyChar == '\n' || e.KeyChar == '\r') {
               e.Handled = true; // prevents a ringing sound from windows
            }
         };
         this.KeyUp += (s, e) => {
            HandleNodeSelectionChanged();
         };
      }

      public event EventHandler<ManagementObjectIdentifierDto> ManagementObjectOpened; 

      private void HandleNodeSelectionChanged() {
         var selectedNode = SelectedNode;
         if (selectedNode == null) return;

         var node = (TrieNode)selectedNode.Tag;
         if (node.ManagementObjectIdentifierDto != null) {
            Console.WriteLine("Hovering mob " + node.ManagementObjectIdentifierDto.FullName);
         }
      }

      private void HandleNodeOpenRequested() {
         var selectedNode = SelectedNode;
         if (selectedNode == null) return;

         var node = (TrieNode)selectedNode.Tag;
         if (node.ManagementObjectIdentifierDto != null) {
            Console.WriteLine("Selected mob " + node.ManagementObjectIdentifierDto.FullName);
            ManagementObjectOpened?.Invoke(this, node.ManagementObjectIdentifierDto);
         }
      }

      public void HandleMobsEnumerated(ManagementObjectIdentifierDto[] mobs) {
         this.BeginInvoke(new Action(() => {
            foreach (var mob in mobs) {
               HandleMobEnumeratedInUiThread(mob);
            }
         }));
      }

      private void HandleMobEnumeratedInUiThread(ManagementObjectIdentifierDto mob) {
         var breadcrumbs = mob.FullName.Split(".");
         
         var current = rootTrieNode;
         foreach (var name in breadcrumbs) {
            if (!current.children.TryGetValue(name, out var newChild)) {
               current.childrenNames.Add(name);

               // crappy linear scan to find our alphabetically-sorted index
               var index = 0;
               foreach (var childName in current.childrenNames) {
                  if (childName == name) break;
                  index++;
               }

               newChild = new TrieNode {
                  WinformsNode = new TreeNode { Text = name },
                  Text = name,
               };
               newChild.WinformsNode.Tag = newChild;
               if (current.WinformsNode is { } currentWinformsNode) {
                  currentWinformsNode.Nodes.Insert(index, newChild.WinformsNode);
               } else {
                  this.Nodes.Insert(index, newChild.WinformsNode);
               }
               current.children.Add(name, newChild);
            }
            current.WinformsNode?.Expand();
            current = newChild;
         }

         current.ManagementObjectIdentifierDto = mob;
      }

      private class TrieNode {
         public TreeNode WinformsNode;
         public string Text;
         public Dictionary<string, TrieNode> children = new();
         public SortedSet<string> childrenNames = new();
         public ManagementObjectIdentifierDto ManagementObjectIdentifierDto;
      }
   }
}
