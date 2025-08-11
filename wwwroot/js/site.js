function initializeTableNavigation(dotNetHelper) {
    document.addEventListener('keydown', function (event) {
        // Проверяем, что фокус находится на одном из наших инпутов
        if (event.target.classList.contains('editable-input')) {
            if (event.key === 'ArrowUp' || event.key === 'ArrowDown') {
                // 1. Предотвращаем стандартное поведение (изменение числа)
                event.preventDefault();

                // 2. Получаем ID текущего элемента
                const currentId = event.target.id;

                // 3. Вызываем C# метод, чтобы он вычислил следующий ID
                dotNetHelper.invokeMethodAsync('GetNextFocusElementId', currentId, event.key)
                    .then(nextId => {
                        if (nextId) {
                            const nextElement = document.getElementById(nextId);
                            if (nextElement) {
                                nextElement.focus();
                                nextElement.select();
                            }
                        }
                    });
            }
        }
    });
}