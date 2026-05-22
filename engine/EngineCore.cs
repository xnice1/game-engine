using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace KripakEngine
{
    // Базовый класс для всех компонентов (просто контейнеры данных)
    public abstract class Component { }

    // Класс сущности (игрок, платформа — это всё Entity)
    public class Entity
    {
        private List<Component> _components = new List<Component>();

        public void AddComponent(Component component) => _components.Add(component);

        // Быстрый поиск нужного компонента у сущности
        public T GetComponent<T>() where T : Component
        {
            foreach (var c in _components)
            {
                if (c is T typedComponent) return typedComponent;
            }
            return null;
        }

        public bool HasComponent<T>() where T : Component => GetComponent<T>() != null;
    }

    public abstract class EngineCore : Game
    {
        protected GraphicsDeviceManager _graphics;
        protected SpriteBatch _spriteBatch;
        protected Texture2D _pixel; // Создаем текстуру прямо в движке, она нужна всем

        // Глобальный список сущностей
        protected List<Entity> _entities = new List<Entity>();

        public EngineCore()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Инициализируем текстуру-плейсхолдер на уровне движка
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }
    }
}