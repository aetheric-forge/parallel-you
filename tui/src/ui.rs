use ratatui::{
    Frame,
    layout::{Alignment, Constraint, Direction, Layout},
    widgets::{Block, Borders, Paragraph},
};

use crate::app::{App, View};

pub fn render(frame: &mut Frame, app: &App) {
    let areas = Layout::default()
        .direction(Direction::Vertical)
        .constraints([
            Constraint::Length(3),
            Constraint::Min(1),
            Constraint::Length(3),
        ])
        .split(frame.area());

    let header = Paragraph::new("1 Library   2 Post Office")
        .alignment(Alignment::Center)
        .block(
            Block::default()
                .title(" Parallel You ")
                .borders(Borders::ALL),
        );

    let workspace = match app.view {
        View::Library => Paragraph::new(
            "MongoDB Library\n\n\
             Provider not connected.",
        )
        .block(Block::default().title(" Library ").borders(Borders::ALL)),

        View::PostOffice => Paragraph::new(
            "RabbitMQ Post Office\n\n\
             Provider not connected.",
        )
        .block(
            Block::default()
                .title(" Post Office ")
                .borders(Borders::ALL),
        ),
    };

    let footer = Paragraph::new("1/2 or Tab: switch   q/Esc: quit")
        .alignment(Alignment::Center)
        .block(Block::default().borders(Borders::ALL));

    frame.render_widget(header, areas[0]);
    frame.render_widget(workspace, areas[1]);
    frame.render_widget(footer, areas[2]);
}
